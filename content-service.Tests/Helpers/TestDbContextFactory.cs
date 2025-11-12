using ContentService.Data;
using Microsoft.EntityFrameworkCore;

namespace ContentService.Tests.Helpers;

public static class TestDbContextFactory
{
    public static ContentDbContext CreateInMemoryContext(string databaseName = "TestDatabase")
    {
        var options = new DbContextOptionsBuilder<ContentDbContext>()
            .UseInMemoryDatabase(databaseName)
            .EnableSensitiveDataLogging()
            .Options;

        var context = new ContentDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }
}
