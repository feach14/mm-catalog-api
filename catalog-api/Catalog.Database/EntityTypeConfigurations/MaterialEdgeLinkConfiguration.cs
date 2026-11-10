using Catalog.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Database.EntityTypeConfigurations;

public sealed class MaterialEdgeLinkConfiguration : IEntityTypeConfiguration<MaterialEdgeLink>
{
    public void Configure(EntityTypeBuilder<MaterialEdgeLink> eb)
    {
        eb.HasKey(x => x.Id);
        eb.HasAlternateKey(x => new { x.OwnerMaterialId, x.EdgeId });
        eb.Property(x => x.OwnerMaterialId)
            .HasComputedColumnSql("COALESCE(material_id, archived_material_id)", stored: true)
            .ValueGeneratedOnAdd()
            .Metadata.SetBeforeSaveBehavior(PropertySaveBehavior.Ignore);
        eb.HasIndex(x => new { x.MaterialId, x.EdgeId }).IsUnique();
        eb.HasIndex(x => new { x.ArchivedMaterialId, x.EdgeId }).IsUnique();
        eb.HasIndex(x => new { x.MaterialId, x.Priority });
        eb.HasIndex(x => new { x.ArchivedMaterialId, x.Priority });
        eb.HasOne(x => x.Material)
            .WithMany()
            .HasForeignKey(x => x.MaterialId)
            .OnDelete(DeleteBehavior.Cascade);
        eb.HasOne(x => x.ArchivedMaterial)
            .WithMany()
            .HasForeignKey(x => x.ArchivedMaterialId)
            .OnDelete(DeleteBehavior.Cascade);
        eb.HasOne(x => x.Edge)
            .WithMany()
            .HasForeignKey(x => x.EdgeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}