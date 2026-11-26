// ReSharper disable NotAccessedPositionalProperty.Global

using Catalog.Api.Features.Materials.Dto;
using Catalog.Api.Features.Materials.Helpers;
using Catalog.Database;
using Catalog.Database.Enums;
using Core.CQRS;

namespace Catalog.Api.Features.Materials.ForCalculate;

public sealed record GetMaterialsForCalculateQuery(
    [property: Description("Поиск по словам в названии, артикуле и характеристиках, до 250 символов"), FromQuery] string? Search = null)
    : IQuery<GetMaterialsForCalculateQueryResult>;

public sealed class GetMaterialsForCalculateQueryValidator : AbstractValidator<GetMaterialsForCalculateQuery>
{
    public GetMaterialsForCalculateQueryValidator()
    {
        RuleFor(x => x.Search)
            .Must(search => search is null || search.Trim().Length <= 250)
            .WithMessage("Поисковая строка не должна превышать 250 символов");
    }
}

public sealed record GetMaterialsForCalculateQueryResult(
    [property: Description("Материалы")] GetMaterialsForCalculateQueryItem[] Items);

public sealed record GetMaterialsForCalculateQueryItem(
    [property: Description("Id")] int Id,
    [property: Description("Полное название")] string Name,
    [property: Description("Артикул")] string Article,
    [property: Description("Категория")] MaterialCategoryForCalculateDto Category,
    [property: Description("Комментарии для клиента")] string[] ClientComments,
    [property: Description("Материал есть в наличии")] bool InStock,
    [property: Description("Признак цены поставщика; true также при выключенной связи")] bool IsSupplierPrice,
    [property: Description("Цены для клиентов")] MaterialClientPricesDto ClientPrices);

public sealed record MaterialCategoryForCalculateDto(
    [property: Description("Id")] int Id,
    [property: Description("Название")] string Name,
    [property: Description("Порядок сортировки")] int OrderByCol);

public sealed class GetMaterialsForCalculateQueryHandler(
    CatalogDbContext dbContext)
    : IQueryHandler<GetMaterialsForCalculateQuery, GetMaterialsForCalculateQueryResult>
{
    public async Task<GetMaterialsForCalculateQueryResult> Handle(GetMaterialsForCalculateQuery query, CancellationToken ct)
    {
        var materials = MaterialSearchQuery.Apply(dbContext.Materials.AsNoTracking(), query.Search);
        var rows = await materials
            .OrderBy(x => x.OrderByCol)
            .Select(x => new
            {
                x.Id,
                InStock = x.Count > 0 && (!x.StroydvorProductId.HasValue || x.StroydvorInStock),
                IsSupplierPrice = !x.StroydvorEnabled || x.StroydvorPrice > 0,
                x.Name,
                x.Article,
                x.Price,
                x.StroydvorPrice,
                MaterialTypeName = x.MaterialType.Name,
                Thickness = x.MaterialThickness.Value,
                x.MaterialSheetSize.Height,
                x.MaterialSheetSize.Width,
                ManufacturerName = x.MaterialManufacturer.Name,
                Category = new MaterialCategoryForCalculateDto(x.CategoryId, x.Category.Name, x.Category.OrderByCol),
                ClientComments = x.Comments.Where(comment => comment.IsForClient).OrderBy(comment => comment.Id)
                    .Select(comment => comment.Text).ToArray()
            })
            .ToArrayAsync(ct);
        var percentages = rows.Length > 0
            ? MaterialClientPricePercentages.From(await dbContext.PricePercentages.AsNoTracking()
                .Where(percentage => percentage.AppliesTo == PricePercentageTargetEnum.MATERIAL)
                .ToDictionaryAsync(percentage => percentage.Key, percentage => percentage.Value, ct))
            : null;
        var items = rows.Select(x => new GetMaterialsForCalculateQueryItem(x.Id,
            MaterialNameHelper.GetFullName(x.Name, x.MaterialTypeName, x.Thickness,
                x.Height, x.Width, x.ManufacturerName), x.Article, x.Category, x.ClientComments, x.InStock, x.IsSupplierPrice,
            percentages!.GetPrices(x.Price, x.StroydvorPrice))).ToArray();

        return new GetMaterialsForCalculateQueryResult(items);
    }
}