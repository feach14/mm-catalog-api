using Catalog.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Database.EntityTypeConfigurations;

public sealed class MaterialEdgeLinkConfiguration : IEntityTypeConfiguration<MaterialEdgeLink>
{
    public void Configure(EntityTypeBuilder<MaterialEdgeLink> eb)
    {
        eb.HasKey(x => new { x.MaterialId, x.EdgeId });
        eb.HasIndex(x => new { x.MaterialId, x.Priority });
        eb.HasOne(x => x.Material)
            .WithMany()
            .HasForeignKey(x => x.MaterialId)
            .OnDelete(DeleteBehavior.Cascade);
        eb.HasOne(x => x.Edge)
            .WithMany()
            .HasForeignKey(x => x.EdgeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}