namespace Catalog.Database.EntityTypeConfigurations;

using Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public sealed class ImageCacheConfiguration : IEntityTypeConfiguration<ImageCache>
{
    public void Configure(EntityTypeBuilder<ImageCache> eb)
    {
        eb.ToTable("image_cache");
        eb.HasKey(x => x.Guid);
        eb.Property(x => x.FileName).HasMaxLength(255);
        eb.Property(x => x.Type).HasMaxLength(30);
    }
}
