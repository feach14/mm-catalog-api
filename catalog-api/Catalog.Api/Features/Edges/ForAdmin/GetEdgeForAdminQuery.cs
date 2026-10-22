using Catalog.Api.Features.Edges.ForAdmin.Dto;
using Catalog.Database;
using Catalog.Database.Enums;
using Core.CQRS;

namespace Catalog.Api.Features.Edges.ForAdmin;

public sealed record GetEdgeForAdminQuery(int Id) : IQuery<GetEdgeForAdminQueryResult>;

public sealed record GetEdgeForAdminQueryResult(
    [property: Description("Id")] int Id,
    [property: Description("Название")] string Name,
    [property: Description("Артикул")] string Article,
    [property: Description("Id производителя")] int ManufacturerId,
    [property: Description("Id толщины")] int ThicknessId,
    [property: Description("Id единицы продажи")] int SaleUnitId,
    [property: Description("Складская позиция")] bool IsStockItem,
    [property: Description("Остаток в метрах")] decimal? StockQuantity,
    [property: Description("Оригинал и миниатюра")] EdgeImagesDto Images);

public sealed class GetEdgeForAdminQueryHandler(CatalogDbContext dbContext) : IQueryHandler<GetEdgeForAdminQuery, GetEdgeForAdminQueryResult>
{
    public async Task<GetEdgeForAdminQueryResult> Handle(GetEdgeForAdminQuery query, CancellationToken ct) =>
        await dbContext.Edges.AsNoTracking()
            .Where(x => x.Id == query.Id)
            .Select(x => new GetEdgeForAdminQueryResult(
                x.Id, x.Name, x.Article, x.ManufacturerId, x.ThicknessId, x.SaleUnitId, x.IsStockItem, x.StockQuantity,
                new EdgeImagesDto(
                    x.Images.Where(image => image.ImageType == EdgeImageTypeEnum.Original).Select(image => (Guid?)image.Guid).SingleOrDefault(),
                    x.Images.Where(image => image.ImageType == EdgeImageTypeEnum.Original).Select(image => new EdgeImageMetadataDto(image.Type, image.Data.Length)).SingleOrDefault(),
                    x.Images.Where(image => image.ImageType == EdgeImageTypeEnum.Thumbnail240).Select(image => (Guid?)image.Guid).SingleOrDefault(),
                    x.Images.Where(image => image.ImageType == EdgeImageTypeEnum.Thumbnail240).Select(image => new EdgeImageMetadataDto(image.Type, image.Data.Length)).SingleOrDefault())))
            .SingleOrDefaultAsync(ct)
        ?? throw new BadHttpRequestException($"Кромка с id={query.Id} не найдена.");
}