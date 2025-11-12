using System.Text;
using ContentService.Configuration;
using ContentService.Data;
using ContentService.Repositories.Impl;
using ContentService.Repositories.Interfaces;
using ContentService.Services.Implementations;
using ContentService.Services.Interfaces;
using ContentService.Validators;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Minio;
using Serilog;

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

        // Configure FluentValidation
        builder.Services.AddFluentValidationAutoValidation();
        builder.Services.AddValidatorsFromAssemblyContaining<CreateProblemRequestValidator>();

        // Configure ContentServiceSettings
        builder.Services.Configure<ContentServiceSettings>(
            builder.Configuration.GetSection("ContentService"));

        // Configure Database Context
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ??
                               throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

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
        var jwtIssuer = builder.Configuration["Jwt:ValidIssuer"] ?? "CodeHakamAuthService";
        var jwtAudience = builder.Configuration["Jwt:ValidAudience"] ?? "CodeHakamContentService";
        var jwtSecretKey = builder.Configuration["Jwt:SecretKey"] ?? "your-secret-key-here-min-32-chars-long-please";

        builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtIssuer,
                    ValidAudience = jwtAudience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey))
                };
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

        // Configure HttpClient
        builder.Services.AddHttpClient();

        var app = builder.Build();

        // Configure the HTTP request pipeline
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.UseDeveloperExceptionPage();
        }
        else
        {
            app.UseExceptionHandler("/error");
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseSerilogRequestLogging();
        app.UseCors("AllowAll");
        app.UseAuthentication();
        app.UseAuthorization();
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
}
