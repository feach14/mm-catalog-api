// ReSharper disable NotAccessedPositionalProperty.Global

using Catalog.Api.Features.Materials.Dto;
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
    [property: Description("Размер материала")] PropertyDto SheetSize,
    [property: Description("Производитель")] PropertyDto Manufacturer,
    [property: Description("Тип материала")] PropertyDto MaterialType,
    [property: Description("Толщина материала")] PropertyDto Thickness,
    [property: Description("Категория")] PropertyDto Category,
    [property: Description("Ссылка на внешний источник")] string? ExternalLink,
    [property: Description("Количество")] int Count,
    [property: Description("Порядковый номер записи (для сортировки)")] int OrderByCol,
    [property: Description("Стоимость материала")] decimal Price,
    [property: Description("Применим к раскрою")] bool ApplicableToRaskroys,
    [property: Description("Применим к фасадам ПВХ")] bool ApplicableToPvhFacades,
    [property: Description("Применим к фасадам эмаль")] bool ApplicableToEmalFacades
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
                new PropertyDto(x.MaterialSheetSize.Id, x.MaterialSheetSize.Name),
                new PropertyDto(x.MaterialManufacturer.Id, x.MaterialManufacturer.Name),
                new PropertyDto(x.MaterialType.Id, x.MaterialType.Name),
                new PropertyDto(x.MaterialThickness.Id, x.MaterialThickness.Name),
                new PropertyDto(x.CategoryId, x.Category.Name),
                x.ExternalLink,
                x.Count,
                x.OrderByCol,
                x.Price,
                x.ApplicableToRaskroys,
                x.ApplicableToPvhFacades,
                x.ApplicableToEmalFacades))
            .ToArrayAsync(ct);

        return new GetAllMaterialsForAdminQueryResult(materials);
    }
}