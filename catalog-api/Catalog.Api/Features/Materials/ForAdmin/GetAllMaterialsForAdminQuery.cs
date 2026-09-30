// ReSharper disable NotAccessedPositionalProperty.Global

using Catalog.Api.Features.Materials.Dto;
using Catalog.Api.Features.MaterialThicknesses.Dto;
using Catalog.Database;
using Core.CQRS;

namespace Catalog.Api.Features.Materials.ForAdmin;

public sealed record GetAllMaterialsForAdminQuery : IQuery<GetAllMaterialsForAdminQueryResult>;

public sealed record GetAllMaterialsForAdminQueryResult(
    [property: Description("Список материалов")] GetMaterialsQueryForAdminItemResult[] Items);

public sealed record GetMaterialsQueryForAdminItemResult(
    [property: Description("Id материала")] int Id,
    [property: Description("Название материала")] string Name,
    [property: Description("Артикул материала")] string Article,
    [property: Description("Размер материала")] MaterialSheetSizeDto SheetSize,
    [property: Description("Производитель")] MaterialManufacturerDto Manufacturer,
    [property: Description("Толщина материала")] MaterialThicknessDto Thickness,
    [property: Description("Категория")] CategoryDto Category,
    [property: Description("Ссылка на внешний источник")] string? ExternalLink,
    [property: Description("Количество")] int Count,
    [property: Description("Порядковый номер записи (для сортировки)")] int OrderByCol,
    [property: Description("Стоимость материала")] decimal Price
);

public class GetAllMaterialsForAdminQueryHandler(CatalogDbContext dbContext) : IQueryHandler<GetAllMaterialsForAdminQuery, GetAllMaterialsForAdminQueryResult>
{
    public async Task<GetAllMaterialsForAdminQueryResult> Handle(GetAllMaterialsForAdminQuery query, CancellationToken ct)
    {
        var materials = await dbContext.Materials.AsNoTracking()
            .OrderBy(x => x.Category.OrderByCol)
            .ThenBy(x => x.OrderByCol)
            .Select(x => new GetMaterialsQueryForAdminItemResult(
                x.Id,
                x.Name,
                x.Article,
                new MaterialSheetSizeDto(
                    x.MaterialSheetSize.Id,
                    x.MaterialSheetSize.Name,
                    x.MaterialSheetSize.Height,
                    x.MaterialSheetSize.Width),
                new MaterialManufacturerDto(x.MaterialManufacturer.Id, x.MaterialManufacturer.Name),
                new MaterialThicknessDto(x.MaterialThickness.Id, x.MaterialThickness.Name, x.MaterialThickness.Value),
                new CategoryDto(x.CategoryId, x.Category.Name),
                x.ExternalLink,
                x.Count,
                x.OrderByCol,
                x.Price))
            .ToArrayAsync(ct);

        return new GetAllMaterialsForAdminQueryResult(materials);
    }
}