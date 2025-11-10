using System.Text;
using AccountService.Configuration;
using AccountService.Data;
using AccountService.Infrastructure;
using AccountService.Models;
using AccountService.Services;
using AccountService.Services.Impl;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using StackExchange.Redis;

namespace AccountService;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Configure Serilog
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .WriteTo.File("logs/account-service-.txt", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        builder.Host.UseSerilog();

        // Add configuration
        builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
        builder.Services.Configure<RedisSettings>(builder.Configuration.GetSection("Redis"));
        builder.Services.Configure<RabbitMqSettings>(builder.Configuration.GetSection("RabbitMq"));
        builder.Services.Configure<RateLimitSettings>(builder.Configuration.GetSection("RateLimiting"));
        builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("Email"));
        builder.Services.Configure<OAuthSettings>(builder.Configuration.GetSection("OAuth"));

        // Add Database Context
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
                // Suppress pending model changes warning in development
                options.ConfigureWarnings(warnings =>
                    warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning));
            }
        });

        // Add Identity
        builder.Services.AddIdentity<User, IdentityRole<long>>(options =>
        {
            // Password settings
            options.Password.RequireDigit = true;
            options.Password.RequireLowercase = true;
            options.Password.RequireUppercase = true;
            options.Password.RequireNonAlphanumeric = true;
            options.Password.RequiredLength = 8;
            options.Password.RequiredUniqueChars = 1;

            // Lockout settings
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.AllowedForNewUsers = true;

            // User settings
            options.User.RequireUniqueEmail = true;
            options.SignIn.RequireConfirmedEmail = false; // Set to true in production
        })
        .AddEntityFrameworkStores<ApplicationDbContext>()
        .AddDefaultTokenProviders();

        // Add JWT Authentication
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
                    var result = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        error = new
                        {
                            code = "UNAUTHORIZED",
                            message = "You are not authorized to access this resource",
                            timestamp = DateTime.UtcNow
                        }
                    });
                    return context.Response.WriteAsync(result);
                }
            };
        });

        // Add Authorization
        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy("RequireUser", policy => policy.RequireAuthenticatedUser());
            options.AddPolicy("RequireAdmin", policy => policy.RequireRole("admin", "super_admin"));
            options.AddPolicy("RequireModerator", policy => policy.RequireRole("moderator", "admin", "super_admin"));
            options.AddPolicy("RequireSetter", policy => policy.RequireRole("setter", "moderator", "admin", "super_admin"));
        });

        // Add Redis
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

        // Add Services
        builder.Services.AddScoped<ITokenService, TokenService>();
        builder.Services.AddScoped<IAuthService, AuthService>();
        builder.Services.AddScoped<IEmailService, EmailService>();
        builder.Services.AddSingleton<IEventPublisher, EventPublisher>();
        builder.Services.AddSingleton<RedisHealthCheck>();

        // Add Controllers
        builder.Services.AddControllers()
            .AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
                options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull;
            });

        // Add CORS
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

        // Add API Documentation
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

            // Add JWT Authentication to Swagger
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
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

        // Add Health Checks
        builder.Services.AddHealthChecks()
            .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection")!)
            .AddCheck<RedisHealthCheck>("redis");

        var app = builder.Build();

        // Configure the HTTP request pipeline
        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "Account Service API v1");
                options.RoutePrefix = "swagger";
            });
        }

        // Apply migrations automatically in development
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

        app.UseSerilogRequestLogging();

        app.UseCors("AllowFrontend");

        app.UseAuthentication();
        app.UseAuthorization();

        // Map health check endpoint
        app.MapHealthChecks("/health");

        // Map controllers
        app.MapControllers();

        // Root endpoint
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
