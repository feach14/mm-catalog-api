namespace Catalog.Database.EntityTypeConfigurations;

using Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class MaterialConfiguration : IEntityTypeConfiguration<Material>
{
    public void Configure(EntityTypeBuilder<Material> eb)
    {
        eb.HasKey(x => x.Id);
        eb.Property(x => x.Article).IsRequired().HasMaxLength(100);
        eb.Property(x => x.Name).IsRequired().HasMaxLength(250);
        eb.Property(x => x.ExternalLink).HasMaxLength(250);
        eb.Property(x => x.OrderByCol).IsRequired().HasDefaultValue(0);
        eb.HasOne(a => a.Category)
            .WithMany(m => m.Materials)
            .HasForeignKey(b => b.CategoryId);
        eb.HasOne(x => x.MaterialSheetSize)
            .WithMany(x => x.Materials)
            .HasForeignKey(x => x.MaterialSheetSizeId)
            .OnDelete(DeleteBehavior.Restrict);
        eb.HasOne(x => x.MaterialManufacturer)
            .WithMany(x => x.Materials)
            .HasForeignKey(x => x.MaterialManufacturerId)
            .OnDelete(DeleteBehavior.Restrict);
        eb.HasMany(x => x.Images)
            .WithOne(x => x.Material)
            .HasForeignKey(x => x.MaterialId);
        eb.HasIndex(x => x.OrderByCol).IsUnique();
    }
}
