namespace Catalog.Database.Entities;

public sealed record EdgeSupplier
{
    public int Id { get; private set; }
    public required string Name { get; set; }
    public bool IncludeInEdgeName { get; set; }
    public int OrderByCol { get; set; }
    public List<Edge> Edges { get; private set; } = [];
}