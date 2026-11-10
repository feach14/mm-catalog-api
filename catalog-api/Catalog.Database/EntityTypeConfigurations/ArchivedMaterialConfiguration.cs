using Catalog.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Database.EntityTypeConfigurations;

public class ArchivedMaterialConfiguration : IEntityTypeConfiguration<ArchivedMaterial>
{
    public void Configure(EntityTypeBuilder<ArchivedMaterial> eb)
    {
        eb.ToTable("archive_materials");
        eb.HasKey(x => x.Id);
        eb.Property(x => x.Id).ValueGeneratedNever();
        eb.Property(x => x.Article).IsRequired().HasMaxLength(100);
        eb.Property(x => x.Name).IsRequired().HasMaxLength(250);
        eb.Property(x => x.ExternalLink).HasMaxLength(250);
        eb.Property(x => x.StroydvorArticle).HasMaxLength(100);
        eb.Property(x => x.StroydvorPrice).HasPrecision(18, 2).HasDefaultValue(0m);
        eb.Property(x => x.StroydvorInStock).HasDefaultValue(false);
        eb.Property(x => x.HideInLk).HasDefaultValue(false);
        eb.HasIndex(x => x.StroydvorProductId);
        eb.Property(x => x.OrderByCol).IsRequired().HasDefaultValue(0);
        eb.HasOne(a => a.Category)
            .WithMany()
            .HasForeignKey(b => b.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
        eb.HasOne(x => x.MaterialSheetSize)
            .WithMany()
            .HasForeignKey(x => x.MaterialSheetSizeId)
            .OnDelete(DeleteBehavior.Restrict);
        eb.HasOne(x => x.MaterialManufacturer)
            .WithMany()
            .HasForeignKey(x => x.MaterialManufacturerId)
            .OnDelete(DeleteBehavior.Restrict);
        eb.HasOne(x => x.MaterialType)
            .WithMany()
            .HasForeignKey(x => x.MaterialTypeId)
            .OnDelete(DeleteBehavior.Restrict);
        eb.HasOne(x => x.MaterialThickness)
            .WithMany()
            .HasForeignKey(x => x.MaterialThicknessId)
            .OnDelete(DeleteBehavior.Restrict);
        eb.HasMany(x => x.Images)
            .WithOne(x => x.ArchivedMaterial)
            .HasForeignKey(x => x.ArchivedMaterialId)
            .OnDelete(DeleteBehavior.Cascade);
        eb.HasIndex(x => x.OrderByCol).IsUnique();
    }
}