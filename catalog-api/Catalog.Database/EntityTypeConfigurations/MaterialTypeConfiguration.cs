using Catalog.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Database.EntityTypeConfigurations;

public sealed class MaterialTypeConfiguration : IEntityTypeConfiguration<MaterialType>
{
    public void Configure(EntityTypeBuilder<MaterialType> eb)
    {
        eb.HasKey(x => x.Id);
        eb.Property(x => x.Name).IsRequired().HasMaxLength(100);
        eb.Property(x => x.OrderByCol).IsRequired().HasDefaultValue(0);
        eb.HasIndex(x => x.OrderByCol).IsUnique();
    }
}