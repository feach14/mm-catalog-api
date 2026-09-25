#pragma warning disable CS8618 // Параметры заполняются на уровне EF 

using System.Diagnostics.CodeAnalysis;
using Catalog.Database.Enums;

namespace Catalog.Database.Entities;

[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
public sealed record Material
{
    public int Id { get; private set; }
    
    public required int CategoryId { get; set; }
    public MaterialCategory Category { get; private set; }

    public required string Name { get; set; }
    public required string Article { get; set; }
    public required string Size { get; set; } // Размер плиты
    public required double Depth { get; set; }
    public required double KvM { get; set; }
    public required double PerimetrM { get; set; }
    public int Count { get; set; }
    public string? ExternalLink { get; set; }
    public bool Deleted { get; set; }
    public bool CommentOnMaterialIsRequired { get; set; }
    public bool AllowSecondItemInOrder { get; set; }
    public required decimal Price { get; set; }
    public required CountTypeEnum CountTypeEnum { get; set; }
    public required int OrderByCol { get; set; }
    
    public bool DefaultPvhFacade { get; private set; }
    public bool DefaultEmalFacade { get; private set; }
    
    public required bool ApplicableToRaskroys { get; set; }
    public required bool ApplicableToPvhFacades { get; set; }
    public required bool ApplicableToEmalFacades { get; set; }
    
    public List<MaterialImage> Images { get; private set; }
}