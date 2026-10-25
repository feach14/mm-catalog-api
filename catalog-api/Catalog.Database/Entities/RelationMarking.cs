namespace Catalog.Database.Entities;

public sealed record RelationMarking
{
    public int Id { get; private set; }
    public required string Name { get; set; }
    public int OrderByCol { get; set; }
    public List<MaterialEdgeLinkMarking> Links { get; private set; } = [];
}