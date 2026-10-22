namespace Catalog.Database.Entities;

public sealed record Edge
{
    public int Id { get; private set; }
    public required string Name { get; set; }
    public required string Article { get; set; }

    public required int ManufacturerId { get; set; }
    public EdgeManufacturer Manufacturer { get; private set; } = null!;

    public required int ThicknessId { get; set; }
    public EdgeThickness Thickness { get; private set; } = null!;

    public required int SaleUnitId { get; set; }
    public EdgeSaleUnit SaleUnit { get; private set; } = null!;

    public required bool IsStockItem { get; set; }
    public decimal? StockQuantity { get; set; }
    public List<EdgeImage> Images { get; private set; } = [];
}