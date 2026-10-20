// ReSharper disable NotAccessedPositionalProperty.Global

using Catalog.Api.Features.Materials.Dto;
using Catalog.Database;
using Core.CQRS;

namespace Catalog.Api.Features.Materials.ForAdmin;

public sealed record GetAllMaterialsForAdminQuery : IQuery<GetAllMaterialsForAdminQueryResult>;

public sealed record GetAllMaterialsForAdminQueryResult(
    [property: Description("Список материалов")] GetMaterialsQueryForAdminItemResult[] Items,
    [property: Description("Справочники для фильтров и редактора")] MaterialsForAdminFiltersDto Filters);

public sealed record MaterialsForAdminFiltersDto(
    [property: Description("Категории")] PropertyDto[] Categories,
    [property: Description("Производители")] PropertyDto[] Manufacturers,
    [property: Description("Типы материалов")] PropertyDto[] MaterialTypes,
    [property: Description("Толщины")] PropertyDto[] Thicknesses,
    [property: Description("Форматы плит")] MaterialsForAdminSheetSizeFilterDto[] SheetSizes);

public sealed record MaterialsForAdminSheetSizeFilterDto(
    [property: Description("Id")] int Id,
    [property: Description("Название")] string Name,
    [property: Description("Высота")] int Height,
    [property: Description("Ширина")] int Width,
    [property: Description("Показывать в фильтрах")] bool ShowInFilters);

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
    [property: Description("Признак: скрыть материал на сайте")] bool HideOnSite,
    [property: Description("Признак: не показывать цену материала на сайте")] bool HidePriceOnSite,
    [property: Description("Использование материала в заявке на распил")] bool ApplicableToCutting,
    [property: Description("Использование материала в заявке на фасады ПВХ")] bool ApplicableToPvhFacades,
    [property: Description("Использование материала в заявке на фасады эмаль")] bool ApplicableToEnamelFacades
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
                x.HideOnSite,
                x.HidePriceOnSite,
                x.ApplicableToCutting,
                x.ApplicableToPvhFacades,
                x.ApplicableToEnamelFacades))
            .ToArrayAsync(ct);

        var categories = await dbContext.MaterialCategories.AsNoTracking()
            .OrderBy(x => x.OrderByCol)
            .Select(x => new PropertyDto(x.Id, x.Name))
            .ToArrayAsync(ct);

        var manufacturers = await dbContext.MaterialManufacturers.AsNoTracking()
            .OrderBy(x => x.OrderByCol)
            .Select(x => new PropertyDto(x.Id, x.Name))
            .ToArrayAsync(ct);

        var materialTypes = await dbContext.MaterialTypes.AsNoTracking()
            .OrderBy(x => x.OrderByCol)
            .Select(x => new PropertyDto(x.Id, x.Name))
            .ToArrayAsync(ct);

        var thicknesses = await dbContext.MaterialThicknesses.AsNoTracking()
            .OrderBy(x => x.Value)
            .Select(x => new PropertyDto(x.Id, x.Name))
            .ToArrayAsync(ct);

        var sheetSizes = await dbContext.MaterialSheetSizes.AsNoTracking()
            .OrderBy(x => x.OrderByCol)
            .Select(x => new MaterialsForAdminSheetSizeFilterDto(
                x.Id,
                x.Name,
                x.Height,
                x.Width,
                x.ShowInFilters))
            .ToArrayAsync(ct);

        return new GetAllMaterialsForAdminQueryResult(materials,
            new MaterialsForAdminFiltersDto(categories, manufacturers, materialTypes, thicknesses, sheetSizes));
    }
}