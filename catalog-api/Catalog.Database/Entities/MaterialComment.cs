namespace Catalog.Database.Entities;

public sealed record MaterialComment
{
    public int Id { get; private set; }
    public int? MaterialId { get; set; }
    public Material? Material { get; private set; }
    public int? ArchivedMaterialId { get; set; }
    public ArchivedMaterial? ArchivedMaterial { get; private set; }
    public required string Text { get; init; }
    public bool IsForClient { get; init; }
}