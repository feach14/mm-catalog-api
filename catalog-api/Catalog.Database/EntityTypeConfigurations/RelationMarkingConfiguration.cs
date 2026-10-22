using Catalog.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Database.EntityTypeConfigurations;

public sealed class RelationMarkingConfiguration : IEntityTypeConfiguration<RelationMarking>
{
    public void Configure(EntityTypeBuilder<RelationMarking> eb)
    {
        eb.HasKey(x => x.Id);
        eb.Property(x => x.Name).IsRequired().HasMaxLength(100);
        eb.Property<string>("NormalizedName").IsRequired().HasMaxLength(100)
            .HasComputedColumnSql("lower(btrim(name))", stored: true);
        eb.HasIndex("NormalizedName").IsUnique();
    }
}