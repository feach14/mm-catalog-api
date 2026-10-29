using Catalog.Database.Enums;

namespace Catalog.Database.Entities;

public sealed record EdgeSaleUnit
{
    public int Id { get; private set; }
    public required string Name { get; set; }
    public required EdgeSaleUnitTypeEnum SaleUnitType { get; set; }
    public required decimal Length { get; set; }
    public List<Edge> Edges { get; private set; } = [];
}