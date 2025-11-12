using AccountService.Data;
using AccountService.Models;
using AccountService.Services;
using AccountService.Services.Impl;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;

namespace AccountService.Tests.Fixtures;

public class AccountServiceFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"TestDb_{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<ApplicationDbContext>();

            services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseInMemoryDatabase(_databaseName);
                options.EnableSensitiveDataLogging();
            });

            services.RemoveAll<StackExchange.Redis.IConnectionMultiplexer>();
            services.AddSingleton<StackExchange.Redis.IConnectionMultiplexer>(_ => null!);

            services.RemoveAll<IEventPublisher>();
            var mockEventPublisher = new Mock<IEventPublisher>();
            services.AddSingleton(mockEventPublisher.Object);

            services.RemoveAll<IEmailService>();
            var mockEmailService = new Mock<IEmailService>();
            mockEmailService
                .Setup(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            mockEmailService
                .Setup(x => x.SendEmailVerificationAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            mockEmailService
                .Setup(x => x.SendPasswordResetAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            mockEmailService
                .Setup(x => x.SendWelcomeEmailAsync(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);
            services.AddSingleton(mockEmailService.Object);

            var hostedServices = services.Where(d => d.ServiceType == typeof(IHostedService)).ToList();
            foreach (var hostedService in hostedServices)
            {
                services.Remove(hostedService);
            }

            services.RemoveAll<ICasbinPolicyService>();
            var mockCasbinPolicyService = new Mock<ICasbinPolicyService>();
            services.AddScoped(_ => mockCasbinPolicyService.Object);

            var serviceProvider = services.BuildServiceProvider();

            using var scope = serviceProvider.CreateScope();
            var scopedServices = scope.ServiceProvider;
            var db = scopedServices.GetRequiredService<ApplicationDbContext>();
            var logger = scopedServices.GetRequiredService<ILogger<AccountServiceFactory>>();

            try
            {
                db.Database.EnsureCreated();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred creating the test database.");
            }
        });

        builder.UseEnvironment("Testing");
    }

    public async Task<User> CreateTestUserAsync(
        string username = "testuser",
        string email = "test@example.com",
        string password = "Test123!@#")
    {
        using var scope = Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

        var user = new User
        {
            UserName = username,
            Email = email,
            IsVerified = true,
            CreatedAt = DateTime.UtcNow
        };

        var result = await userManager.CreateAsync(user, password);

        if (!result.Succeeded)
        {
            throw new InvalidOperationException(
                $"Failed to create test user: {string.Join(", ", result.Errors.Select(e => e.Description))}");
        }

        return user;
    }

    public async Task<string> GetAccessTokenAsync(User user)
    {
        using var scope = Services.CreateScope();
        var tokenService = scope.ServiceProvider.GetRequiredService<ITokenService>();
        return await tokenService.GenerateAccessTokenAsync(user, new List<string>());
    }
}
