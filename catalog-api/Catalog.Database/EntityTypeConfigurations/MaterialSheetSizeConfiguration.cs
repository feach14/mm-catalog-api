namespace Catalog.Database.EntityTypeConfigurations;

using Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class MaterialSheetSizeConfiguration : IEntityTypeConfiguration<MaterialSheetSize>
{
    public void Configure(EntityTypeBuilder<MaterialSheetSize> eb)
    {
        eb.HasKey(x => x.Id);
        eb.Property(x => x.Name).IsRequired().HasMaxLength(100);
        eb.Property(x => x.Height).IsRequired();
        eb.Property(x => x.Width).IsRequired();
        eb.Property(x => x.OrderByCol).IsRequired().HasDefaultValue(0);
        eb.HasIndex(x => new { x.Height, x.Width }).IsUnique();
        eb.HasIndex(x => x.OrderByCol).IsUnique();
    }
}
