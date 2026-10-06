using System.Diagnostics.CodeAnalysis;
using Core.BaseEnums;

#pragma warning disable CS8618 // Параметры заполняются на уровне EF

namespace Catalog.Database.Entities;

[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
public sealed record Material
{
    public int Id { get; private set; }

    public required int CategoryId { get; set; }
    public MaterialCategory Category { get; private set; }

    public required int MaterialSheetSizeId { get; set; }
    public MaterialSheetSize MaterialSheetSize { get; private set; }

    public required int MaterialTypeId { get; set; }
    public MaterialType MaterialType { get; private set; }

    public required int MaterialManufacturerId { get; set; }
    public MaterialManufacturer MaterialManufacturer { get; private set; }

    public required string Name { get; set; }
    public required string Article { get; set; }
    public required int MaterialThicknessId { get; set; }
    public MaterialThickness MaterialThickness { get; private set; }

    public required decimal KvM { get; set; }
    public required decimal PerimetrM { get; set; }
    public int Count { get; set; }
    public string? ExternalLink { get; set; }
    public bool CommentOnMaterialIsRequired { get; set; }
    public bool AllowSecondItemInOrder { get; set; }
    public required decimal Price { get; set; }
    public bool HideOnSite { get; set; }
    public bool HidePriceOnSite { get; set; }
    public required CountTypeEnum CountTypeEnum { get; set; }
    public required int OrderByCol { get; set; }

    public required bool ApplicableToRaskroys { get; set; }
    public required bool ApplicableToPvhFacades { get; set; }
    public required bool ApplicableToEmalFacades { get; set; }

    public List<MaterialImage> Images { get; private set; }
}