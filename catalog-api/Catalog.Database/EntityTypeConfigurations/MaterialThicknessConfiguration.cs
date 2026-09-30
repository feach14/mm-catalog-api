using Catalog.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Database.EntityTypeConfigurations;

public sealed class MaterialThicknessConfiguration : IEntityTypeConfiguration<MaterialThickness>
{
    public void Configure(EntityTypeBuilder<MaterialThickness> eb)
    {
        eb.HasKey(x => x.Id);
        eb.Property(x => x.Name).IsRequired().HasMaxLength(100);
        eb.Property(x => x.Value).HasPrecision(18, 3);
        eb.HasIndex(x => x.Value).IsUnique();
    }
}