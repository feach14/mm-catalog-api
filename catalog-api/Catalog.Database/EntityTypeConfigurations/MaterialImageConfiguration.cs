namespace Catalog.Database.EntityTypeConfigurations;

using Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class MaterialImageConfiguration : IEntityTypeConfiguration<MaterialImage>
{
    public void Configure(EntityTypeBuilder<MaterialImage> eb)
    {
        eb.HasKey(x => x.Id);
        eb.Property(x => x.Type).HasMaxLength(30);
        eb.HasOne(x => x.Material)
            .WithMany(x => x.Images)
            .HasForeignKey(x => x.MaterialId);
    }
}