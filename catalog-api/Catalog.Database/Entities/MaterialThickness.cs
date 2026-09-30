#pragma warning disable CS8618

namespace Catalog.Database.Entities;

public sealed record MaterialThickness
{
    public int Id { get; private set; }
    public required string Name { get; set; }
    public required decimal Value { get; set; }
    public List<Material> Materials { get; private set; }
}