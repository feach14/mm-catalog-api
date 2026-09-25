using Catalog.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Database.EntityTypeConfigurations;

public class MaterialCategoryConfiguration : IEntityTypeConfiguration<MaterialCategory>
{
    public void Configure(EntityTypeBuilder<MaterialCategory> eb)
    {
        eb.HasKey(x => x.Id);
        eb.Property(x => x.Name).IsRequired().HasMaxLength(200);
        eb.Property(x => x.ExternalLink).IsRequired().HasMaxLength(250);
        eb.Property(x => x.OrderByCol).IsRequired();
        eb.HasMany(x => x.Materials)
            .WithOne(x => x.Category)
            .HasForeignKey(x => x.CategoryId);
        
        eb.HasIndex(x => x.OrderByCol).IsUnique();
    }
}
