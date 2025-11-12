using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ContentService.Data;

public class ContentDbContextFactory : IDesignTimeDbContextFactory<ContentDbContext>
{
    public ContentDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ContentDbContext>();

        var connectionString = "Host=localhost;Port=5432;Database=codehakam;Username=postgres;Password=postgres;SearchPath=content";

        optionsBuilder.UseNpgsql(connectionString);

        return new ContentDbContext(optionsBuilder.Options);
    }
}
