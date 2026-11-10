using Catalog.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Database.EntityTypeConfigurations;

public sealed class MaterialEdgeLinkMarkingConfiguration : IEntityTypeConfiguration<MaterialEdgeLinkMarking>
{
    public void Configure(EntityTypeBuilder<MaterialEdgeLinkMarking> eb)
    {
        eb.HasKey(x => x.Id);
        eb.HasIndex(x => new { x.MaterialId, x.EdgeId, x.MarkingId }).IsUnique();
        eb.HasOne(x => x.Link)
            .WithMany(x => x.Markings)
            .HasForeignKey(x => new { x.MaterialId, x.EdgeId })
            .HasPrincipalKey(x => new { x.OwnerMaterialId, x.EdgeId })
            .OnDelete(DeleteBehavior.Cascade);
        eb.HasOne(x => x.Marking)
            .WithMany(x => x.Links)
            .HasForeignKey(x => x.MarkingId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}