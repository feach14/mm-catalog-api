namespace Catalog.Database.Entities;

public sealed record EdgeThickness
{
    public int Id { get; private set; }
    public required string Name { get; set; }
    public required decimal Value { get; set; }
    public List<Edge> Edges { get; private set; } = [];
}