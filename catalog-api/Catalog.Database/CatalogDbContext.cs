using System.Reflection;
using Catalog.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Database;

// dotnet ef migrations add <название_миграции> --project Catalog.Database
// dotnet ef database update  --project Catalog.Database
// dotnet ef migrations remove --project Catalog.Database

public class CatalogDbContext(DbContextOptions<CatalogDbContext> _options) : DbContext(_options)
{
    public const string SchemaName = "catalog";

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(SchemaName);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
    
    public DbSet<Material> Materials { get; init; }
    public DbSet<MaterialCategory> MaterialCategories { get; init; }
    public DbSet<MaterialImage> MaterialImages { get; init; }
}
