using Catalog.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Database.EntityTypeConfigurations;

public sealed class EdgeThicknessConfiguration : IEntityTypeConfiguration<EdgeThickness>
{
    public void Configure(EntityTypeBuilder<EdgeThickness> eb)
    {
        eb.HasKey(x => x.Id);
        eb.Property(x => x.Name).IsRequired().HasMaxLength(100);
        eb.Property<string>("NormalizedName").IsRequired().HasMaxLength(100)
            .HasComputedColumnSql("lower(btrim(name))", stored: true);
        eb.Property(x => x.Value).HasPrecision(18, 3);
        eb.HasIndex("NormalizedName").IsUnique();
        eb.HasIndex(x => x.Value).IsUnique();
    }
}