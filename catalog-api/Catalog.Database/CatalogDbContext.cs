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

    public DbSet<MaterialType> MaterialTypes { get; init; }
    public DbSet<MaterialThickness> MaterialThicknesses { get; init; }
    public DbSet<Material> Materials { get; init; }
    public DbSet<MaterialSheetSize> MaterialSheetSizes { get; init; }
    public DbSet<MaterialManufacturer> MaterialManufacturers { get; init; }
    public DbSet<MaterialCategory> MaterialCategories { get; init; }
    public DbSet<MaterialImage> MaterialImages { get; init; }
    public DbSet<ImageCache> ImageCache { get; init; }
    public DbSet<Edge> Edges { get; init; }
    public DbSet<EdgeManufacturer> EdgeManufacturers { get; init; }
    public DbSet<EdgeThickness> EdgeThicknesses { get; init; }
    public DbSet<EdgeSaleUnit> EdgeSaleUnits { get; init; }
    public DbSet<EdgeImage> EdgeImages { get; init; }
    public DbSet<EdgeImageCache> EdgeImageCache { get; init; }
    public DbSet<RelationMarking> RelationMarkings { get; init; }
    public DbSet<MaterialEdgeLink> MaterialEdgeLinks { get; init; }
    public DbSet<MaterialEdgeLinkMarking> MaterialEdgeLinkMarkings { get; init; }
    public DbSet<CatalogChangeHistory> ChangeHistory { get; init; }
}