using Catalog.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Database.EntityTypeConfigurations;

public sealed class EdgeImageConfiguration : IEntityTypeConfiguration<EdgeImage>
{
    public void Configure(EntityTypeBuilder<EdgeImage> eb)
    {
        eb.HasKey(x => x.Id);
        eb.Property(x => x.Type).IsRequired().HasMaxLength(30);
        eb.Property(x => x.Data).IsRequired();
        eb.Property(x => x.ImageType).HasConversion<string>().HasMaxLength(30).IsRequired();
        eb.HasIndex(x => x.Guid).IsUnique();
        eb.HasIndex(x => new { x.EdgeId, x.ImageType }).IsUnique();
        eb.HasOne(x => x.Edge)
            .WithMany(x => x.Images)
            .HasForeignKey(x => x.EdgeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}