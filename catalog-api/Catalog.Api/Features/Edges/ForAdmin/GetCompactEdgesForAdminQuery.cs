using Catalog.Api.Features.Edges.Dto;
using Catalog.Api.Features.Edges.ForAdmin.Dto;
using Catalog.Api.Features.Edges.Helpers;
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
    [property: Description("Id товара Стройдвора")] int? StroydvorProductId,
    [property: Description("Артикул Стройдвора")] string? StroydvorArticle,
    [property: Description("Цена Стройдвора: за метр для Meter, за бухту для Roll, руб.")] decimal StroydvorPrice,
    [property: Description("Дата успешной синхронизации со Стройдвором")] DateTimeOffset? StroydvorLastSyncAt,
    [property: Description("В наличии в Стройдворе")] bool StroydvorInStock,
    [property: Description("Id производителя")] int ManufacturerId,
    [property: Description("Id поставщика")] int SupplierId,
    [property: Description("Id размера")] int SizeId,
    [property: Description("Id единицы продажи")] int SaleUnitId,
    [property: Description("Складская позиция")] bool IsStockItem,
    [property: Description("Остаток в метрах")] decimal? StockQuantity,
    [property: Description("Порядок сортировки")] int OrderByCol,
    [property: Description("GUID миниатюры")] Guid? Thumbnail240,
    [property: Description("Комментарии")] EdgeCommentDto[] Comments,
    [property: Description("Цены для клиентов; null при неизвестной наценке толщины")] EdgeClientPricesDto? ClientPrices);

public sealed record EdgesForAdminFiltersDto(
    [property: Description("Производители")] EdgeFilterItemDto[] Manufacturers,
    [property: Description("Поставщики")] EdgeSupplierFilterItemDto[] Suppliers,
    [property: Description("Размеры")] EdgeSizeFilterItemDto[] Sizes,
    [property: Description("Единицы продажи")] EdgeSaleUnitFilterItemDto[] SaleUnits);

public sealed record EdgeFilterItemDto(
    [property: Description("Id")] int Id,
    [property: Description("Название")] string Name);

public sealed record EdgeSupplierFilterItemDto(
    [property: Description("Id")] int Id,
    [property: Description("Название")] string Name,
    [property: Description("Добавлять поставщика в полное название кромки")] bool IncludeInEdgeName,
    [property: Description("Порядок сортировки")] int OrderByCol);

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
                x.Id, x.Name, x.Article, x.Price,
                x.StroydvorProductId, x.StroydvorArticle, x.StroydvorPrice, x.StroydvorLastSyncAt, x.StroydvorInStock,
                x.ManufacturerId, x.SupplierId, x.SizeId, x.SaleUnitId,
                x.IsStockItem, x.StockQuantity, x.OrderByCol,
                x.Images.Where(image => image.ImageType == EdgeImageTypeEnum.Thumbnail240)
                    .Select(image => (Guid?)image.Guid).SingleOrDefault(),
                x.Comments.OrderBy(comment => comment.Id)
                    .Select(comment => new EdgeCommentDto(comment.Id, comment.Text, comment.IsForClient)).ToArray(),
                null))
            .ToArrayAsync(ct);
        var manufacturers = await dbContext.EdgeManufacturers.AsNoTracking()
            .OrderBy(x => x.Name).ThenBy(x => x.Id)
            .Select(x => new EdgeFilterItemDto(x.Id, x.Name)).ToArrayAsync(ct);
        var sizes = await dbContext.EdgeSizes.AsNoTracking()
            .OrderBy(x => x.OrderByCol)
            .Select(x => new EdgeSizeFilterItemDto(x.Id, x.Name, x.Thickness, x.Width, x.OrderByCol)).ToArrayAsync(ct);
        var suppliers = await dbContext.EdgeSuppliers.AsNoTracking()
            .OrderBy(x => x.OrderByCol)
            .Select(x => new EdgeSupplierFilterItemDto(x.Id, x.Name, x.IncludeInEdgeName, x.OrderByCol)).ToArrayAsync(ct);
        var saleUnits = await dbContext.EdgeSaleUnits.AsNoTracking()
            .OrderBy(x => x.OrderByCol)
            .Select(x => new EdgeSaleUnitFilterItemDto(x.Id, x.Name, x.SaleUnitType)).ToArrayAsync(ct);
        if (items.Length > 0)
        {
            var percentageValues = await dbContext.PricePercentages.AsNoTracking()
                .Where(percentage => percentage.AppliesTo == PricePercentageTargetEnum.EDGE)
                .ToDictionaryAsync(percentage => percentage.Key, percentage => percentage.Value, ct);
            var percentages = EdgeClientPricePercentages.From(percentageValues);
            var thicknesses = sizes.ToDictionary(size => size.Id, size => size.Thickness);
            items = items.Select(edge => edge with
            {
                ClientPrices = percentages.GetPrices(thicknesses[edge.SizeId], edge.Price, edge.StroydvorPrice)
            }).ToArray();
        }

        return new GetCompactEdgesForAdminQueryResult(items, new EdgesForAdminFiltersDto(manufacturers, suppliers, sizes, saleUnits));
    }
}