namespace Catalog.Database.Entities;

public sealed record MaterialEdgeLink
{
    public required int MaterialId { get; init; }
    public Material Material { get; private set; } = null!;

    public required int EdgeId { get; init; }
    public Edge Edge { get; private set; } = null!;

    public required int Priority { get; set; }
    public List<MaterialEdgeLinkMarking> Markings { get; private set; } = [];
}