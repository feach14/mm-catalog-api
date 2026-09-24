using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Catalog.Database;

internal class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<CatalogDbContext>
{
    private const string EfMigrationsHistoryTableName = "__EFMigrationsHistory";
    private const string DbConnString = "Host=185.151.240.63;Database=mm-debug;Username=postgres;Password=Rus140589!";

    public CatalogDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<CatalogDbContext>();
        optionsBuilder
            .UseNpgsql(DbConnString, options => { options.MigrationsHistoryTable(EfMigrationsHistoryTableName, CatalogDbContext.SchemaName); })
            .UseSnakeCaseNamingConvention();

        return new CatalogDbContext(optionsBuilder.Options);
    }
}
