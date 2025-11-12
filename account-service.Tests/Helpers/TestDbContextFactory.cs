using AccountService.Data;
using Microsoft.EntityFrameworkCore;

namespace AccountService.Tests.Helpers;

public static class TestDbContextFactory
{
    public static ApplicationDbContext CreateInMemoryContext(string databaseName)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(databaseName: databaseName)
            .EnableSensitiveDataLogging()
            .Options;

        var context = new ApplicationDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    public static ApplicationDbContext CreatePostgresContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connectionString, npgsqlOptions =>
            {
                npgsqlOptions.MigrationsHistoryTable("__ef_migrations_history", "users");
                npgsqlOptions.CommandTimeout(30);
            })
            .EnableSensitiveDataLogging()
            .EnableDetailedErrors()
            .Options;

        var context = new ApplicationDbContext(options);
        context.Database.Migrate();
        return context;
    }
}
