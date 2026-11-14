using Catalog.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Database.EntityTypeConfigurations;

public sealed class EdgeSizeConfiguration : IEntityTypeConfiguration<EdgeSize>
{
    public void Configure(EntityTypeBuilder<EdgeSize> eb)
    {
        eb.ToTable("edge_sizes");
        eb.HasKey(x => x.Id);
        eb.Property(x => x.Name).IsRequired().HasMaxLength(100);
        eb.Property(x => x.Thickness).HasColumnName("thickness").HasPrecision(18, 3);
        eb.Property(x => x.Width).HasPrecision(18, 3).HasDefaultValue(0m);
        eb.Property(x => x.OrderByCol).HasDefaultValue(0);
        eb.HasIndex(x => new { x.OrderByCol, x.Id });
        eb.HasIndex(x => new { x.Thickness, x.Width }).HasDatabaseName("ix_edge_sizes_thickness_width").IsUnique();
    }
}