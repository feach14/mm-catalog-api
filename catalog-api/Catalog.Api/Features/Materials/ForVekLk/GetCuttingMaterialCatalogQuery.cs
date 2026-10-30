// ReSharper disable NotAccessedPositionalProperty.Global

using Catalog.Database;
using Catalog.Database.Enums;
using Core.CQRS;

namespace Catalog.Api.Features.Materials.ForVekLk;

public sealed record GetCuttingMaterialCatalogQuery : IQuery<GetCuttingMaterialCatalogQueryResult>;

public sealed record GetCuttingMaterialCatalogQueryResult(
    [property: Description("Материалы для раскроя")] CuttingMaterialCatalogItemDto[] Items,
    [property: Description("Категории материалов")] CuttingMaterialCatalogCategoryDto[] Categories);

public sealed record CuttingMaterialCatalogItemDto(
    [property: Description("Id")] int Id,
    [property: Description("Название")] string Name,
    [property: Description("Артикул")] string Article,
    [property: Description("Id категории")] int CategoryId,
    [property: Description("Количество")] int Count,
    [property: Description("GUID миниатюры: 240 пикселей, иначе 480")] Guid? Image);

public sealed record CuttingMaterialCatalogCategoryDto(
    [property: Description("Id")] int Id,
    [property: Description("Название")] string Name);

public sealed class GetCuttingMaterialCatalogQueryHandler(CatalogDbContext dbContext)
    : IQueryHandler<GetCuttingMaterialCatalogQuery, GetCuttingMaterialCatalogQueryResult>
{
    public async Task<GetCuttingMaterialCatalogQueryResult> Handle(GetCuttingMaterialCatalogQuery query, CancellationToken ct)
    {
        var materials = await dbContext.Materials.AsNoTracking()
            .Where(x => !x.HideInLk && !x.Category.HideInLk)
            .OrderBy(x => x.Category.OrderByCol).ThenBy(x => x.CategoryId)
            .ThenBy(x => x.OrderByCol).ThenBy(x => x.Id)
            .Select(x => new
            {
                x.Id,
                x.Name,
                MaterialTypeName = x.MaterialType.Name,
                Thickness = x.MaterialThickness.Value,
                x.MaterialSheetSize.Height,
                x.MaterialSheetSize.Width,
                ManufacturerName = x.MaterialManufacturer.Name,
                x.Article,
                x.CategoryId,
                CategoryName = x.Category.Name,
                CategoryOrderByCol = x.Category.OrderByCol,
                x.Count,
                Image = x.Images
                    .Where(image => image.ImageType == MaterialImageTypeEnum.Thumbnail240
                        || image.ImageType == MaterialImageTypeEnum.Thumbnail480)
                    .OrderBy(image => image.ImageType == MaterialImageTypeEnum.Thumbnail240 ? 0 : 1)
                    .ThenBy(image => image.Id)
                    .Select(image => (Guid?)image.Guid)
                    .FirstOrDefault()
            })
            .ToArrayAsync(ct);
        var items = materials.Select(x => new CuttingMaterialCatalogItemDto(
            x.Id,
            VekLkMaterialName.Format(x.MaterialTypeName, x.Name, x.Thickness, x.Height, x.Width, x.ManufacturerName),
            x.Article, x.CategoryId, x.Count, x.Image)).ToArray();
        var categories = materials
            .GroupBy(x => new { x.CategoryId, x.CategoryName, x.CategoryOrderByCol })
            .OrderBy(x => x.Key.CategoryOrderByCol).ThenBy(x => x.Key.CategoryId)
            .Select(x => new CuttingMaterialCatalogCategoryDto(x.Key.CategoryId, x.Key.CategoryName))
            .ToArray();

        return new GetCuttingMaterialCatalogQueryResult(items, categories);
    }
}