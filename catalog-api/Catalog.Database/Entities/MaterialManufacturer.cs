#pragma warning disable CS8618

namespace Catalog.Database.Entities;

public sealed record MaterialManufacturer
{
    public int Id { get; private set; }
    public required string Name { get; set; }
    public required int OrderByCol { get; set; }
    public List<Material> Materials { get; private set; }
}