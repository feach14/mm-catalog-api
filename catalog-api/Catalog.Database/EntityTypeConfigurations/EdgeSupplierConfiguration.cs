using Catalog.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Database.EntityTypeConfigurations;

public sealed class EdgeSupplierConfiguration : IEntityTypeConfiguration<EdgeSupplier>
{
    public void Configure(EntityTypeBuilder<EdgeSupplier> eb)
    {
        eb.HasKey(x => x.Id);
        eb.Property(x => x.Name).IsRequired().HasMaxLength(100);
        eb.Property<string>("NormalizedName").IsRequired().HasMaxLength(100)
            .HasComputedColumnSql("lower(btrim(name))", stored: true);
        eb.HasIndex("NormalizedName").IsUnique();
        eb.Property(x => x.IncludeInEdgeName).HasDefaultValue(false);
        eb.Property(x => x.OrderByCol).HasDefaultValue(0);
        eb.HasIndex(x => new { x.OrderByCol, x.Id });
    }
}