using Catalog.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Database.EntityTypeConfigurations;

public sealed class EdgeSaleUnitConfiguration : IEntityTypeConfiguration<EdgeSaleUnit>
{
    public void Configure(EntityTypeBuilder<EdgeSaleUnit> eb)
    {
        eb.HasKey(x => x.Id);
        eb.Property(x => x.Name).IsRequired().HasMaxLength(100);
        eb.Property<string>("NormalizedName").IsRequired().HasMaxLength(100)
            .HasComputedColumnSql("lower(btrim(name))", stored: true);
        eb.Property(x => x.Type).HasConversion<string>().HasMaxLength(30).IsRequired();
        eb.Property(x => x.Length).HasPrecision(18, 3);
        eb.HasIndex("NormalizedName").IsUnique();
        eb.HasIndex(x => new { x.Type, x.Length }).IsUnique();
    }
}