using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using ContentService.Configuration;
using ContentService.Data;
using ContentService.Middleware;
using ContentService.Repositories.Impl;
using ContentService.Repositories.Interfaces;
using ContentService.Services.BackgroundServices;
using ContentService.Services.Implementations;
using ContentService.Services.Interfaces;
using ContentService.Validators;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Minio;
using Serilog;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace ContentService;

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
            .WriteTo.File("logs/content-service-.txt", rollingInterval: RollingInterval.Day)
            .CreateLogger();

        builder.Host.UseSerilog();

        // Add services to the container
        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddOpenApi();

        // Add Swagger/OpenAPI
        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new()
            {
                Title = "CodeHakam Content Service API",
                Version = "v1",
                Description = "Content Service API for managing problems, test cases, editorials, discussions, and problem lists"
            });

            c.DocumentFilter<LowercaseDocumentFilter>();
        });

        // Configure FluentValidation
        builder.Services.AddFluentValidationAutoValidation();
        builder.Services.AddValidatorsFromAssemblyContaining<CreateProblemRequestValidator>();

        // Configure ContentServiceSettings
        builder.Services.Configure<ContentServiceSettings>(
            builder.Configuration.GetSection("ContentService"));

        // Configure Database Context
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
                               throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        // Add Health Checks
        builder.Services.AddHealthChecks()
            .AddNpgSql(
                connectionString,
                name: "postgres",
                failureStatus: HealthStatus.Unhealthy,
                tags: new[] { "db", "sql", "postgres" });

        builder.Services.AddDbContext<ContentDbContext>(options =>
            options.UseNpgsql(connectionString));

        // Configure MinIO
        var minioEndpoint = builder.Configuration["MinIO:Endpoint"] ?? "localhost:9000";
        var minioAccessKey = builder.Configuration["MinIO:AccessKey"] ?? "minioadmin";
        var minioSecretKey = builder.Configuration["MinIO:SecretKey"] ?? "minioadmin";
        var minioUseSSL = bool.Parse(builder.Configuration["MinIO:UseSSL"] ?? "false");

        builder.Services.AddSingleton<IMinioClient>(sp =>
        {
            return new MinioClient()
                .WithEndpoint(minioEndpoint)
                .WithCredentials(minioAccessKey, minioSecretKey)
                .WithSSL(minioUseSSL)
                .Build();
        });

        // Configure JWT Authentication
        var jwtIssuer = builder.Configuration["JwtSettings:Issuer"] ?? "codehakam";
        var jwtAudience = builder.Configuration["JwtSettings:Audience"] ?? "codehakam-api";
        var jwtSecretKey = builder.Configuration["JwtSettings:SecretKey"] ?? "your-secret-key-here-minimum-32-characters-long-for-security";

        builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;

                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey));

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtIssuer,
                    ValidAudience = jwtAudience,
                    IssuerSigningKey = key,
                    ClockSkew = TimeSpan.Zero
                };

                options.SecurityTokenValidators.Clear();
                options.SecurityTokenValidators.Add(new JwtSecurityTokenHandler
                {
                    MapInboundClaims = false
                });
            });

        builder.Services.AddAuthorization();

        // Configure CORS
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowAll", policy =>
            {
                policy.AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader();
            });
        });

        // Register Repositories
        builder.Services.AddScoped<IProblemRepository, ProblemRepository>();
        builder.Services.AddScoped<ITestCaseRepository, TestCaseRepository>();
        builder.Services.AddScoped<IEditorialRepository, EditorialRepository>();
        builder.Services.AddScoped<IDiscussionRepository, DiscussionRepository>();
        builder.Services.AddScoped<IProblemListRepository, ProblemListRepository>();

        // Register Services
        builder.Services.AddScoped<IProblemService, ProblemService>();
        builder.Services.AddScoped<ITestCaseService, TestCaseService>();
        builder.Services.AddScoped<IEditorialService, EditorialService>();
        builder.Services.AddScoped<IDiscussionService, DiscussionService>();
        builder.Services.AddScoped<IProblemListService, ProblemListService>();

        // Register Infrastructure Services
        builder.Services.AddSingleton<IStorageService, MinIoStorageService>();
        builder.Services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();

        // Register Background Services
        builder.Services.AddHostedService<UserEventConsumer>();

        // Configure HttpClient
        builder.Services.AddHttpClient();

        var app = builder.Build();

        // Configure the HTTP request pipeline
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "Content Service API v1");
                c.RoutePrefix = "swagger";
            });
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseSerilogRequestLogging();

        app.UseMiddleware<ExceptionHandlingMiddleware>();
        app.UseCors("AllowAll");
        app.UseAuthentication();
        app.UseAuthorization();

        // Map health check endpoints
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = async (context, report) =>
            {
                context.Response.ContentType = "application/json";
                var result = JsonSerializer.Serialize(new
                {
                    status = report.Status.ToString(),
                    checks = report.Entries.Select(e => new
                    {
                        name = e.Key,
                        status = e.Value.Status.ToString(),
                        description = e.Value.Description,
                        duration = e.Value.Duration.TotalMilliseconds
                    }),
                    totalDuration = report.TotalDuration.TotalMilliseconds
                });
                await context.Response.WriteAsync(result);
            }
        });

        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("db")
        });

        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false
        });

        app.MapControllers();

        // Ensure MinIO bucket exists at startup
        using (var scope = app.Services.CreateScope())
        {
            var storageService = scope.ServiceProvider.GetRequiredService<IStorageService>();
            var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            var bucketName = configuration["MinIO:BucketName"] ?? "codehakam-testcases";

            try
            {
                storageService.EnsureBucketExistsAsync(bucketName).Wait();
                Log.Information("MinIO bucket '{BucketName}' is ready", bucketName);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Failed to ensure MinIO bucket exists. Service will continue but storage operations may fail.");
            }
        }

        // Run database migrations if in development
        if (app.Environment.IsDevelopment())
        {
            using (var scope = app.Services.CreateScope())
            {
                try
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<ContentDbContext>();
                    dbContext.Database.Migrate();
                    Log.Information("Database migrations applied successfully");
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "An error occurred while migrating the database");
                }
            }
        }

        Log.Information("Content Service started successfully");

        try
        {
            app.Run();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }

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
}
