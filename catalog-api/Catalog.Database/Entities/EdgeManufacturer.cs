namespace Catalog.Database.Entities;

public sealed record EdgeManufacturer
{
    public int Id { get; private set; }
    public required string Name { get; set; }
    public List<Edge> Edges { get; private set; } = [];
}