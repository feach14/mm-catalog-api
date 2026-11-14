using Catalog.Database;
using Catalog.Database.Enums;
using Core.CQRS;

namespace Catalog.Api.Features.Edges.ForAdmin;

public sealed record GetCompactEdgesForAdminQuery : IQuery<GetCompactEdgesForAdminQueryResult>;

public sealed record GetCompactEdgesForAdminQueryResult(
    [property: Description("Список кромок")] GetCompactEdgesForAdminQueryItem[] Items,
    [property: Description("Справочники для фильтров и редактора")] EdgesForAdminFiltersDto Filters);

public sealed record GetCompactEdgesForAdminQueryItem(
    [property: Description("Id")] int Id,
    [property: Description("Название")] string Name,
    [property: Description("Артикул")] string Article,
    [property: Description("Стоимость: за метр для Meter, за бухту для Roll, руб.")] decimal Price,
    [property: Description("Id производителя")] int ManufacturerId,
    [property: Description("Id размера")] int SizeId,
    [property: Description("Id единицы продажи")] int SaleUnitId,
    [property: Description("Складская позиция")] bool IsStockItem,
    [property: Description("Остаток в метрах")] decimal? StockQuantity,
    [property: Description("Порядок сортировки")] int OrderByCol,
    [property: Description("GUID миниатюры")] Guid? Thumbnail240);

public sealed record EdgesForAdminFiltersDto(
    [property: Description("Производители")] EdgeFilterItemDto[] Manufacturers,
    [property: Description("Размеры")] EdgeSizeFilterItemDto[] Sizes,
    [property: Description("Единицы продажи")] EdgeSaleUnitFilterItemDto[] SaleUnits);

public sealed record EdgeFilterItemDto(
    [property: Description("Id")] int Id,
    [property: Description("Название")] string Name);

public sealed record EdgeSaleUnitFilterItemDto(
    [property: Description("Id")] int Id,
    [property: Description("Название")] string Name,
    [property: Description("Тип: Meter — погонный метр, Roll — бухта"), JsonConverter(typeof(StrictStringEnumConverter))] EdgeSaleUnitTypeEnum SaleUnitType);

public sealed record EdgeSizeFilterItemDto(
    [property: Description("Id")] int Id,
    [property: Description("Название")] string Name,
    [property: Description("Толщина в миллиметрах")] decimal Thickness,
    [property: Description("Ширина в миллиметрах")] decimal Width,
    [property: Description("Порядок сортировки")] int OrderByCol);

public sealed class GetCompactEdgesForAdminQueryHandler(CatalogDbContext dbContext)
    : IQueryHandler<GetCompactEdgesForAdminQuery, GetCompactEdgesForAdminQueryResult>
{
    public async Task<GetCompactEdgesForAdminQueryResult> Handle(GetCompactEdgesForAdminQuery query, CancellationToken ct)
    {
        var items = await dbContext.Edges.AsNoTracking()
            .OrderBy(x => x.OrderByCol)
            .Select(x => new GetCompactEdgesForAdminQueryItem(
                x.Id, x.Name, x.Article, x.Price, x.ManufacturerId, x.SizeId, x.SaleUnitId,
                x.IsStockItem, x.StockQuantity, x.OrderByCol,
                x.Images.Where(image => image.ImageType == EdgeImageTypeEnum.Thumbnail240)
                    .Select(image => (Guid?)image.Guid).SingleOrDefault()))
            .ToArrayAsync(ct);
        var manufacturers = await dbContext.EdgeManufacturers.AsNoTracking()
            .OrderBy(x => x.Name).ThenBy(x => x.Id)
            .Select(x => new EdgeFilterItemDto(x.Id, x.Name)).ToArrayAsync(ct);
        var sizes = await dbContext.EdgeSizes.AsNoTracking()
            .OrderBy(x => x.OrderByCol)
            .Select(x => new EdgeSizeFilterItemDto(x.Id, x.Name, x.Thickness, x.Width, x.OrderByCol)).ToArrayAsync(ct);
        var saleUnits = await dbContext.EdgeSaleUnits.AsNoTracking()
            .OrderBy(x => x.OrderByCol)
            .Select(x => new EdgeSaleUnitFilterItemDto(x.Id, x.Name, x.SaleUnitType)).ToArrayAsync(ct);
        return new GetCompactEdgesForAdminQueryResult(items, new EdgesForAdminFiltersDto(manufacturers, sizes, saleUnits));
    }
}