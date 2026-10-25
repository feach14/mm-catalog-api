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
    [property: Description("Id производителя")] int ManufacturerId,
    [property: Description("Id толщины")] int ThicknessId,
    [property: Description("Id единицы продажи")] int SaleUnitId,
    [property: Description("Складская позиция")] bool IsStockItem,
    [property: Description("Остаток в метрах")] decimal? StockQuantity,
    [property: Description("Порядок сортировки")] int OrderByCol,
    [property: Description("GUID миниатюры")] Guid? Thumbnail240);

public sealed record EdgesForAdminFiltersDto(
    [property: Description("Производители")] EdgeFilterItemDto[] Manufacturers,
    [property: Description("Толщины")] EdgeThicknessFilterItemDto[] Thicknesses,
    [property: Description("Единицы продажи")] EdgeFilterItemDto[] SaleUnits);

public sealed record EdgeFilterItemDto(
    [property: Description("Id")] int Id,
    [property: Description("Название")] string Name);

public sealed record EdgeThicknessFilterItemDto(
    [property: Description("Id")] int Id,
    [property: Description("Название")] string Name,
    [property: Description("Толщина в миллиметрах")] decimal Value);

public sealed class GetCompactEdgesForAdminQueryHandler(CatalogDbContext dbContext)
    : IQueryHandler<GetCompactEdgesForAdminQuery, GetCompactEdgesForAdminQueryResult>
{
    public async Task<GetCompactEdgesForAdminQueryResult> Handle(GetCompactEdgesForAdminQuery query, CancellationToken ct)
    {
        var items = await dbContext.Edges.AsNoTracking()
            .OrderBy(x => x.OrderByCol).ThenBy(x => x.Id)
            .Select(x => new GetCompactEdgesForAdminQueryItem(
                x.Id, x.Name, x.Article, x.ManufacturerId, x.ThicknessId, x.SaleUnitId,
                x.IsStockItem, x.StockQuantity, x.OrderByCol,
                x.Images.Where(image => image.ImageType == EdgeImageTypeEnum.Thumbnail240)
                    .Select(image => (Guid?)image.Guid).SingleOrDefault()))
            .ToArrayAsync(ct);
        var manufacturers = await dbContext.EdgeManufacturers.AsNoTracking()
            .OrderBy(x => x.Name).ThenBy(x => x.Id)
            .Select(x => new EdgeFilterItemDto(x.Id, x.Name)).ToArrayAsync(ct);
        var thicknesses = await dbContext.EdgeThicknesses.AsNoTracking()
            .OrderBy(x => x.Value).ThenBy(x => x.Id)
            .Select(x => new EdgeThicknessFilterItemDto(x.Id, x.Name, x.Value)).ToArrayAsync(ct);
        var saleUnits = await dbContext.EdgeSaleUnits.AsNoTracking()
            .OrderBy(x => x.Name).ThenBy(x => x.Id)
            .Select(x => new EdgeFilterItemDto(x.Id, x.Name)).ToArrayAsync(ct);
        return new GetCompactEdgesForAdminQueryResult(items, new EdgesForAdminFiltersDto(manufacturers, thicknesses, saleUnits));
    }
}