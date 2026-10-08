using Catalog.Database.Entities;
using Catalog.Database.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Database.EntityTypeConfigurations;

public sealed class ImageCacheConfiguration : IEntityTypeConfiguration<ImageCache>
{
    public void Configure(EntityTypeBuilder<ImageCache> eb)
    {
        eb.ToTable("image_cache");
        eb.HasKey(x => x.Guid);
        eb.Property(x => x.FileName).HasMaxLength(255);
        eb.Property(x => x.Type).HasMaxLength(30);
        eb.Property(x => x.ImageType)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired()
            .HasSentinel(default(MaterialImageTypeEnum))
            .HasDefaultValue(MaterialImageTypeEnum.Original);
        eb.HasIndex(x => x.ImageType);
    }
}