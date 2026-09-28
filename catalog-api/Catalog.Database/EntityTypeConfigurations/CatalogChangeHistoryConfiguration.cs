using Catalog.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Database.EntityTypeConfigurations;

public sealed class CatalogChangeHistoryConfiguration : IEntityTypeConfiguration<CatalogChangeHistory>
{
    public void Configure(EntityTypeBuilder<CatalogChangeHistory> eb)
    {
        eb.HasKey(x => x.Id);
        eb.Property(x => x.ActionType).HasConversion<string>().HasMaxLength(16);
        eb.Property(x => x.EntityType).HasConversion<string>().HasMaxLength(32);
        eb.Property(x => x.UserPhone).HasMaxLength(32);
        eb.Property(x => x.Message).HasColumnType("text");
        eb.HasIndex(x => x.OccurredAt);
        eb.HasIndex(x => x.ActionType);
        eb.HasIndex(x => new { x.EntityType, x.EntityId });
        eb.HasIndex(x => x.UserPhone);
    }
}