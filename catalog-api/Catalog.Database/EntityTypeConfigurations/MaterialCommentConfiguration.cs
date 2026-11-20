using Catalog.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Database.EntityTypeConfigurations;

public sealed class MaterialCommentConfiguration : IEntityTypeConfiguration<MaterialComment>
{
    public void Configure(EntityTypeBuilder<MaterialComment> eb)
    {
        eb.HasKey(x => x.Id);
        eb.Property(x => x.Text).HasMaxLength(50).IsRequired();
        eb.HasIndex(x => new { x.MaterialId, x.Id });
        eb.HasIndex(x => new { x.ArchivedMaterialId, x.Id });
        eb.HasOne(x => x.Material)
            .WithMany(x => x.Comments)
            .HasForeignKey(x => x.MaterialId)
            .OnDelete(DeleteBehavior.Cascade);
        eb.HasOne(x => x.ArchivedMaterial)
            .WithMany(x => x.Comments)
            .HasForeignKey(x => x.ArchivedMaterialId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}