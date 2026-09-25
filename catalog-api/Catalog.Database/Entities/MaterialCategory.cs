#pragma warning disable CS8618 // Параметры заполняются на уровне EF 

using System.Diagnostics.CodeAnalysis;

namespace Catalog.Database.Entities;

[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
public sealed record MaterialCategory
{
    public int Id { get; private set; }
    public List<Material> Materials { get; private set; }
    
    public required string Name { get; set; }
    public required int OrderByCol { get; set; }
    
    public required string ExternalLink { get; set; }
}