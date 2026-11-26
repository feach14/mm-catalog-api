// ReSharper disable NotAccessedPositionalProperty.Global

using Catalog.Api.Features.Materials.Dto;
using Catalog.Api.Features.Materials.Helpers;
using Catalog.Database;
using Catalog.Database.Enums;
using Core.CQRS;

namespace Catalog.Api.Features.Materials.Public;

public sealed record GetAllMaterialsQuery : IQuery<GetAllMaterialsQueryResult>;

public sealed record GetAllMaterialsQueryResult(
    [property: Description("Список материалов")] GetAllMaterialsQueryItemResult[] Items);

public sealed record GetAllMaterialsQueryItemResult(
    [property: Description("Id материала")] int Id,
    [property: Description("Название материала")] string Name,
    [property: Description("Артикул материала")] string Article,
    [property: Description("Размер материала")] PropertyDto SheetSize,
    [property: Description("Производитель")] PropertyDto Manufacturer,
    [property: Description("Тип материала")] PropertyDto MaterialType,
    [property: Description("Толщина материала")] PropertyDto Thickness,
    [property: Description("Категория")] PropertyDto Category,
    [property: Description("Количество")] int Count,
    [property: Description("Признак: Комментарий к материалу обязателен при оформлении заявки(расчета)")] bool CommentOnMaterialIsRequired,
    [property: Description("Порядковый номер записи (для сортировки)")] int OrderByCol,
    [property: Description("Комментарии для клиента")] string[] ClientComments,
    [property: Description("Материал есть в наличии")] bool InStock,
    [property: Description("Признак цены поставщика; true также при выключенной связи")] bool IsSupplierPrice,
    [property: Description("Цены для клиентов; null при скрытой цене")] MaterialClientPricesDto? ClientPrices);

public class GetAllMaterialsQueryHandler(
    CatalogDbContext dbContext) : IQueryHandler<GetAllMaterialsQuery, GetAllMaterialsQueryResult>
{
    public async Task<GetAllMaterialsQueryResult> Handle(GetAllMaterialsQuery query, CancellationToken ct)
    {
        var rows = await dbContext.Materials.AsNoTracking()
            .Where(x => !x.HideOnSite && !x.Category.HideOnSite && !x.MaterialType.HideOnSite)
            .OrderBy(x => x.OrderByCol)
            .Select(x => new
            {
                Material = new GetAllMaterialsQueryItemResult(
                    x.Id,
                    x.Name,
                    x.Article,
                    new PropertyDto(x.MaterialSheetSize.Id, x.MaterialSheetSize.Name),
                    new PropertyDto(x.MaterialManufacturer.Id, x.MaterialManufacturer.Name),
                    new PropertyDto(x.MaterialType.Id, x.MaterialType.Name),
                    new PropertyDto(x.MaterialThickness.Id, x.MaterialThickness.Name),
                    new PropertyDto(x.CategoryId, x.Category.Name),
                    x.Count,
                    x.CommentOnMaterialIsRequired,
                    x.OrderByCol,
                    x.Comments.Where(comment => comment.IsForClient).OrderBy(comment => comment.Id)
                        .Select(comment => comment.Text).ToArray(),
                    x.Count > 0 && (!x.StroydvorProductId.HasValue || x.StroydvorInStock),
                    !x.StroydvorEnabled || x.StroydvorPrice > 0,
                    null),
                x.Price,
                x.StroydvorPrice,
                x.HidePriceOnSite
            })
            .ToArrayAsync(ct);

        var percentages = rows.Any(row => !row.HidePriceOnSite)
            ? MaterialClientPricePercentages.From(await dbContext.PricePercentages.AsNoTracking()
                .Where(percentage => percentage.AppliesTo == PricePercentageTargetEnum.MATERIAL)
                .ToDictionaryAsync(percentage => percentage.Key, percentage => percentage.Value, ct))
            : null;
        var materials = rows.Select(row => row.Material with
        {
            ClientPrices = row.HidePriceOnSite ? null : percentages?.GetPrices(row.Price, row.StroydvorPrice)
        }).ToArray();

        return new GetAllMaterialsQueryResult(materials);
    }
}