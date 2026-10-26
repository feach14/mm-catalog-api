using Catalog.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Database.EntityTypeConfigurations;

public sealed class EdgeArticleReplacementConfiguration : IEntityTypeConfiguration<EdgeArticleReplacement>
{
    public void Configure(EntityTypeBuilder<EdgeArticleReplacement> eb)
    {
        eb.HasKey(x => x.Id);
        eb.Property(x => x.OldArticle).IsRequired().HasMaxLength(100);
        eb.HasIndex(x => x.OldArticle).IsUnique();
        eb.HasOne(x => x.Edge)
            .WithMany(x => x.ArticleReplacements)
            .HasForeignKey(x => x.EdgeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}