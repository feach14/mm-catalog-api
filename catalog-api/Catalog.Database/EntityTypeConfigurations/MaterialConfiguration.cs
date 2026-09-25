using Catalog.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Database.EntityTypeConfigurations;

public class MaterialConfiguration : IEntityTypeConfiguration<Material>
{
    public void Configure(EntityTypeBuilder<Material> eb)
    {
        eb.HasKey(x => x.Id);
        eb.Property(x => x.Article).IsRequired().HasMaxLength(100);
        eb.Property(x => x.Name).IsRequired().HasMaxLength(250);
        eb.Property(x => x.Size).IsRequired().HasMaxLength(100);
        eb.Property(x => x.ExternalLink).HasMaxLength(250);
        eb.Property(x => x.OrderByCol).IsRequired().HasDefaultValue(0);
        eb.HasOne(a => a.Category)
            .WithMany(m => m.Materials)
            .HasForeignKey(b => b.CategoryId);
        eb.HasMany(x => x.Images)
            .WithOne(x => x.Material)
            .HasForeignKey(x => x.MaterialId);
        eb.HasIndex(x => x.OrderByCol);
    }
}
