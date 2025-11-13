using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using AccountService.Services.BackgroundServices;
using AccountService.Configuration;
using AccountService.Data;
using AccountService.DTOs;
using AccountService.DTOs.Common;
using AccountService.Infrastructure;
using AccountService.Middleware;
using AccountService.Models;
using AccountService.Services.Interfaces;
using AccountService.Services.Implementations;
using Casbin;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using StackExchange.Redis;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace AccountService;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.File("logs/account-service-.txt", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        builder.Host.UseSerilog();

        builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
        builder.Services.Configure<RedisSettings>(builder.Configuration.GetSection("Redis"));
        builder.Services.Configure<RabbitMqSettings>(builder.Configuration.GetSection("RabbitMq"));
        builder.Services.Configure<RateLimitSettings>(builder.Configuration.GetSection("RateLimiting"));
        builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("Email"));
        builder.Services.Configure<OAuthSettings>(builder.Configuration.GetSection("OAuth"));

        builder.Services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseNpgsql(
                builder.Configuration.GetConnectionString("DefaultConnection"),
                npgsqlOptions =>
                {
                    npgsqlOptions.MigrationsHistoryTable("__ef_migrations_history", "users");
                    npgsqlOptions.CommandTimeout(30);
                });

            if (builder.Environment.IsDevelopment())
            {
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
                options.ConfigureWarnings(warnings =>
                    warnings.Ignore(RelationalEventId.PendingModelChangesWarning));
            }
        });

        builder.Services.AddIdentity<User, IdentityRole<long>>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequiredLength = 8;
                options.Password.RequiredUniqueChars = 1;

                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.AllowedForNewUsers = true;

                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedEmail = false; // Set to true in production
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        var jwtSettings = builder.Configuration.GetSection("JwtSettings").Get<JwtSettings>();
        if (jwtSettings == null)
        {
            throw new InvalidOperationException("JwtSettings configuration is missing");
        }

        var key = Encoding.UTF8.GetBytes(jwtSettings.SecretKey);

        builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.SaveToken = true;
                options.RequireHttpsMetadata = !builder.Environment.IsDevelopment();
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidAudience = jwtSettings.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ClockSkew = TimeSpan.Zero
                };

                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
                        {
                            context.Response.Headers.Append("Token-Expired", "true");
                        }

                        return Task.CompletedTask;
                    },
                    OnChallenge = context =>
                    {
                        context.HandleResponse();
                        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        context.Response.ContentType = "application/json";
                        var result = JsonSerializer.Serialize(
                            ApiResponse<object>.ErrorResponse("You are not authorized to access this resource")
                        );
                        return context.Response.WriteAsync(result);
                    }
                };
            });

        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy("RequireUser", policy => policy.RequireAuthenticatedUser());
            options.AddPolicy("RequireAdmin", policy => policy.RequireRole("admin", "super_admin"));
            options.AddPolicy("RequireModerator", policy => policy.RequireRole("moderator", "admin", "super_admin"));
            options.AddPolicy("RequireSetter",
                policy => policy.RequireRole("setter", "moderator", "admin", "super_admin"));
        });

        builder.Services.AddScoped<ICasbinPolicyService, CasbinPolicyService>();

        builder.Services.AddSingleton<IEnforcer>(serviceProvider =>
        {
            var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
            var modelPath = Path.Combine(AppContext.BaseDirectory, "casbin_model.conf");

            if (!File.Exists(modelPath))
            {
                logger.LogWarning("Casbin model file not found at {ModelPath}", modelPath);
            }

            var enforcer = new Enforcer(modelPath);
            enforcer.EnableAutoSave(false);
            enforcer.EnableAutoBuildRoleLinks(true);

            logger.LogInformation("Casbin enforcer initialized with model: {ModelPath}", modelPath);

            return enforcer;
        });

        var redisSettings = builder.Configuration.GetSection("Redis").Get<RedisSettings>();
        if (redisSettings != null && !string.IsNullOrEmpty(redisSettings.ConnectionString))
        {
            try
            {
                var redisConfig = ConfigurationOptions.Parse(redisSettings.ConnectionString);
                if (!string.IsNullOrEmpty(redisSettings.Password))
                {
                    redisConfig.Password = redisSettings.Password;
                }

                redisConfig.ConnectTimeout = redisSettings.ConnectTimeout;
                redisConfig.SyncTimeout = redisSettings.SyncTimeout;
                redisConfig.AbortOnConnectFail = redisSettings.AbortOnConnectFail;

                builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConfig));
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to connect to Redis. Caching will be disabled.");
                builder.Services.AddSingleton<IConnectionMultiplexer>(_ => null!);
            }
        }

        builder.Services.AddScoped<ITokenService, TokenService>();
        builder.Services.AddScoped<IAuthService, AuthService>();
        builder.Services.AddScoped<IEmailService, EmailService>();
        builder.Services.AddScoped<IUserService, UserService>();
        builder.Services.AddScoped<IAdminService, AdminService>();
        builder.Services.AddScoped<IEventPublisher, OutboxEventPublisherService>();
        builder.Services.AddSingleton<RedisHealthCheck>();
        builder.Services.AddHostedService<PolicySyncService>();
        builder.Services.AddHostedService<OutboxEventPublisher>();

        builder.Services.AddControllers()
            .ConfigureApiBehaviorOptions(options =>
            {
                // Suppress automatic 400 response for model validation errors
                // Let controllers handle validation errors with consistent ApiResponse format
                options.SuppressModelStateInvalidFilter = true;
            })
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            });

        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowFrontend", policy =>
            {
                var frontendUrl = builder.Configuration["FrontendUrl"] ?? "http://localhost:3000";
                policy.WithOrigins(frontendUrl)
                    .AllowAnyMethod()
                    .AllowAnyHeader()
                    .AllowCredentials();
            });
        });

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "CodeHakam Account Service API",
                Version = "v1",
                Description = "Account Service manages user accounts, authentication, and authorization",
                Contact = new OpenApiContact
                {
                    Name = "CodeHakam Team",
                    Email = "support@codehakam.com"
                }
            });

            // Use lowercase URLs in Swagger
            options.DocumentFilter<LowercaseDocumentFilter>();

            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description =
                    "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });

        builder.Services.AddHealthChecks()
            .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection")!)
            .AddCheck<RedisHealthCheck>("redis");

        var app = builder.Build();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "Account Service API v1");
                options.RoutePrefix = "swagger";
                options.DocumentTitle = "CodeHakam Account Service API";
                options.DisplayRequestDuration();
            });
        }

        if (app.Environment.IsDevelopment())
        {
            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            try
            {
                db.Database.Migrate();
                Log.Information("Database migrations applied successfully");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error applying database migrations");
            }
        }

        var casbinModelPath = Path.Combine(AppContext.BaseDirectory, "casbin_model.conf");
        if (!File.Exists(casbinModelPath))
        {
            Log.Warning("casbin_model.conf not found at {Path}. RBAC enforcement may not work correctly.",
                casbinModelPath);
        }

        app.UseSerilogRequestLogging();

        app.UseMiddleware<ExceptionHandlingMiddleware>();

        app.UseCors("AllowFrontend");

        app.UseAuthentication();
        app.UseAuthorization();

        app.MapHealthChecks("/health");

        app.MapControllers();

        app.MapGet("/", () => new
        {
            service = "CodeHakam Account Service",
            version = "1.0.0",
            status = "running",
            timestamp = DateTime.UtcNow
        });

        Log.Information("Starting Account Service on port 3001");

        app.Run();
    }
}

/// <summary>
///     Swagger document filter to convert all URLs to lowercase
/// </summary>
public class LowercaseDocumentFilter : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        var pathsToModify = swaggerDoc.Paths.ToDictionary(
            entry => LowercasePathPreservingParameters(entry.Key),
            entry => entry.Value
        );

        swaggerDoc.Paths.Clear();
        foreach (var path in pathsToModify)
        {
            swaggerDoc.Paths.Add(path.Key, path.Value);
        }
    }

    private static string LowercasePathPreservingParameters(string path)
    {
        var regex = new Regex(@"\{[^}]+\}");
        var parameters = regex.Matches(path);
        var result = path.ToLowerInvariant();

        foreach (Match match in parameters)
        {
            var original = match.Value;
            var lowercased = original.ToLowerInvariant();
            if (original != lowercased)
            {
                result = result.Replace(lowercased, original);
            }
        }

        return result;
    }
}
