namespace Catalog.Database.Entities;

public sealed record MaterialEdgeLinkMarking
{
    public required int MaterialId { get; init; }
    public required int EdgeId { get; init; }
    public MaterialEdgeLink Link { get; private set; } = null!;

    public required int MarkingId { get; init; }
    public RelationMarking Marking { get; private set; } = null!;
}