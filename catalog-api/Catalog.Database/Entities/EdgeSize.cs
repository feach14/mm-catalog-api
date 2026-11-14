namespace Catalog.Database.Entities;

public sealed record EdgeSize
{
    public int Id { get; private set; }
    public required string Name { get; set; }
    public required decimal Thickness { get; set; }
    public required decimal Width { get; set; }
    public int OrderByCol { get; set; }
    public List<Edge> Edges { get; private set; } = [];
}