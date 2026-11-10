namespace Catalog.Database.Entities;

public sealed record MaterialEdgeLink
{
    public int Id { get; private set; }
    public int OwnerMaterialId { get; private set; }

    public int? MaterialId { get; set; }
    public Material? Material { get; private set; }
    public int? ArchivedMaterialId { get; set; }
    public ArchivedMaterial? ArchivedMaterial { get; private set; }

    public required int EdgeId { get; init; }
    public Edge Edge { get; private set; } = null!;

    public required int Priority { get; set; }
    public List<MaterialEdgeLinkMarking> Markings { get; private set; } = [];
}