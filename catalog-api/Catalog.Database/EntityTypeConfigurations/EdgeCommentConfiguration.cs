using Catalog.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Database.EntityTypeConfigurations;

public sealed class EdgeCommentConfiguration : IEntityTypeConfiguration<EdgeComment>
{
    public void Configure(EntityTypeBuilder<EdgeComment> eb)
    {
        eb.HasKey(x => x.Id);
        eb.Property(x => x.Text).HasMaxLength(50).IsRequired();
        eb.Property(x => x.IsForClient).HasDefaultValue(false);
        eb.HasIndex(x => new { x.EdgeId, x.Id });
        eb.HasOne(x => x.Edge)
            .WithMany(x => x.Comments)
            .HasForeignKey(x => x.EdgeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}