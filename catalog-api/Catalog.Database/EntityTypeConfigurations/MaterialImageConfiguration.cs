using Catalog.Database.Entities;
using Catalog.Database.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Database.EntityTypeConfigurations;

public class MaterialImageConfiguration : IEntityTypeConfiguration<MaterialImage>
{
    public void Configure(EntityTypeBuilder<MaterialImage> eb)
    {
        eb.HasKey(x => x.Id);
        eb.Property(x => x.Type).HasMaxLength(30);
        eb.Property(x => x.ImageType)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired()
            .HasDefaultValue(MaterialImageTypeEnum.Original);
        eb.HasIndex(x => x.ImageType);
        eb.HasIndex(x => new { x.MaterialId, x.ImageType }).IsUnique();
        eb.HasOne(x => x.Material)
            .WithMany(x => x.Images)
            .HasForeignKey(x => x.MaterialId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}