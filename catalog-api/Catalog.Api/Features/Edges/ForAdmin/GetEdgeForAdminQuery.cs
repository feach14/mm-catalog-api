using Catalog.Api.Features.Edges.Dto;
using Catalog.Api.Features.Edges.ForAdmin.Dto;
using Catalog.Api.Features.Edges.Helpers;
using Catalog.Database;
using Catalog.Database.Enums;
using Core.CQRS;

namespace Catalog.Api.Features.Edges.ForAdmin;

public sealed record GetEdgeForAdminQuery(int Id) : IQuery<GetEdgeForAdminQueryResult>;

public sealed record GetEdgeForAdminQueryResult(
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
    [property: Description("Оригинал и миниатюра")] EdgeImagesDto Images,
    [property: Description("Комментарии")] EdgeCommentDto[] Comments,
    [property: Description("Цены для клиентов; null при неизвестной наценке толщины")] EdgeClientPricesDto? ClientPrices);

public sealed class GetEdgeForAdminQueryHandler(CatalogDbContext dbContext)
    : IQueryHandler<GetEdgeForAdminQuery, GetEdgeForAdminQueryResult>
{
    public async Task<GetEdgeForAdminQueryResult> Handle(GetEdgeForAdminQuery query, CancellationToken ct)
    {
        var row = await dbContext.Edges.AsNoTracking()
            .Where(x => x.Id == query.Id)
            .Select(x => new
            {
                Edge = new GetEdgeForAdminQueryResult(
                    x.Id, x.Name, x.Article, x.Price,
                    x.StroydvorProductId, x.StroydvorArticle, x.StroydvorPrice, x.StroydvorLastSyncAt, x.StroydvorInStock,
                    x.ManufacturerId, x.SupplierId, x.SizeId, x.SaleUnitId, x.IsStockItem, x.StockQuantity, x.OrderByCol,
                    new EdgeImagesDto(
                        x.Images.Where(image => image.ImageType == EdgeImageTypeEnum.Original).Select(image => (Guid?)image.Guid).SingleOrDefault(),
                        x.Images.Where(image => image.ImageType == EdgeImageTypeEnum.Original).Select(image => new EdgeImageMetadataDto(image.ContentType, image.Data.Length)).SingleOrDefault(),
                        x.Images.Where(image => image.ImageType == EdgeImageTypeEnum.Thumbnail240).Select(image => (Guid?)image.Guid).SingleOrDefault(),
                        x.Images.Where(image => image.ImageType == EdgeImageTypeEnum.Thumbnail240).Select(image => new EdgeImageMetadataDto(image.ContentType, image.Data.Length)).SingleOrDefault()),
                    x.Comments.OrderBy(comment => comment.Id)
                        .Select(comment => new EdgeCommentDto(comment.Id, comment.Text, comment.IsForClient)).ToArray(),
                    null),
                x.Size.Thickness
            })
            .SingleOrDefaultAsync(ct)
            ?? throw new BadHttpRequestException($"Кромка с id={query.Id} не найдена.");

        var percentageValues = await dbContext.PricePercentages.AsNoTracking()
            .Where(percentage => percentage.AppliesTo == PricePercentageTargetEnum.EDGE)
            .ToDictionaryAsync(percentage => percentage.Key, percentage => percentage.Value, ct);
        var percentages = EdgeClientPricePercentages.From(percentageValues);
        return row.Edge with { ClientPrices = percentages.GetPrices(row.Thickness, row.Edge.Price, row.Edge.StroydvorPrice) };
    }
}