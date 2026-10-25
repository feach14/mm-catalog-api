using Catalog.Database.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Catalog.Database.EntityTypeConfigurations;

public sealed class EdgeConfiguration : IEntityTypeConfiguration<Edge>
{
    public void Configure(EntityTypeBuilder<Edge> eb)
    {
        eb.HasKey(x => x.Id);
        eb.Property(x => x.Name).IsRequired().HasMaxLength(250);
        eb.Property(x => x.Article).IsRequired().HasMaxLength(100);
        eb.Property(x => x.StockQuantity).HasPrecision(18, 3);
        eb.Property(x => x.OrderByCol).HasDefaultValue(0);
        eb.HasIndex(x => new { x.OrderByCol, x.Id });
        eb.HasOne(x => x.Manufacturer)
            .WithMany(x => x.Edges)
            .HasForeignKey(x => x.ManufacturerId)
            .OnDelete(DeleteBehavior.Restrict);
        eb.HasOne(x => x.Thickness)
            .WithMany(x => x.Edges)
            .HasForeignKey(x => x.ThicknessId)
            .OnDelete(DeleteBehavior.Restrict);
        eb.HasOne(x => x.SaleUnit)
            .WithMany(x => x.Edges)
            .HasForeignKey(x => x.SaleUnitId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}