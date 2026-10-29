using Catalog.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Database.EntityTypeConfigurations;

public sealed class EdgeImageCacheConfiguration : IEntityTypeConfiguration<EdgeImageCache>
{
    public void Configure(EntityTypeBuilder<EdgeImageCache> eb)
    {
        eb.ToTable("edge_image_cache");
        eb.HasKey(x => x.Guid);
        eb.Property(x => x.FileName).IsRequired().HasMaxLength(255);
        eb.Property(x => x.ContentType).IsRequired().HasMaxLength(30);
        eb.Property(x => x.Data).IsRequired();
        eb.Property(x => x.ImageType).HasConversion<string>().HasMaxLength(30).IsRequired();
    }
}