// ReSharper disable NotAccessedPositionalProperty.Global

using Catalog.Api.Features.Materials.Dto;
using Catalog.Api.Features.Materials.ForAdmin.Dto;
using Catalog.Api.Features.Materials.Helpers;
using Catalog.Database;
using Catalog.Database.Enums;
using Core.CQRS;

namespace Catalog.Api.Features.Materials.ForAdmin;

public sealed record GetCompactMaterialsForAdminQuery : IQuery<GetCompactMaterialsForAdminQueryResult>;

public sealed record GetCompactMaterialsForAdminQueryResult(
    [property: Description("Список материалов")] GetCompactMaterialsForAdminQueryItem[] Items,
    [property: Description("Справочники для фильтров и редактора")] MaterialsForAdminFiltersDto Filters);

public sealed record GetCompactMaterialsForAdminQueryItem(
    [property: Description("Id")] int I,
    [property: Description("Название")] string N,
    [property: Description("Артикул")] string A,
    [property: Description("Id формата плиты")] int S,
    [property: Description("Id производителя")] int M,
    [property: Description("Id типа материала")] int T,
    [property: Description("Id толщины")] int Th,
    [property: Description("Id категории")] int C,
    [property: Description("Количество")] int Q,
    [property: Description("Порядок сортировки")] int O,
    [property: Description("Стоимость")] decimal P,
    [property: Description("Скрыть в ЛК")] bool HideInLk,
    [property: Description("Скрыть на сайте")] bool H,
    [property: Description("Не показывать цену на сайте")] bool Hp,
    [property: Description("GUID миниатюры: 240 пикселей, иначе 480")] Guid? Im,
    [property: Description("Ссылка на внешний источник")] string? El,
    [property: Description("Связь со Стройдвором включена")] bool StroydvorEnabled,
    [property: Description("Id товара Стройдвора")] int? Spid,
    [property: Description("Артикул Стройдвора")] string? Spa,
    [property: Description("Цена Стройдвора за лист")] decimal Spp,
    [property: Description("Дата успешной синхронизации со Стройдвором")] DateTimeOffset? Spls,
    [property: Description("В наличии в Стройдворе")] bool Spis,
    [property: Description("Техническая ссылка на товар Стройдвора")] string? Spurl,
    [property: Description("Комментарии")] MaterialCommentDto[] Comments,
    [property: Description("Цены для клиентов")] MaterialClientPricesDto? ClientPrices);

public sealed class GetCompactMaterialsForAdminQueryHandler(CatalogDbContext dbContext)
    : IQueryHandler<GetCompactMaterialsForAdminQuery, GetCompactMaterialsForAdminQueryResult>
{
    public async Task<GetCompactMaterialsForAdminQueryResult> Handle(GetCompactMaterialsForAdminQuery query, CancellationToken ct)
    {
        var items = await dbContext.Materials.AsNoTracking()
            .OrderBy(x => x.OrderByCol)
            .Select(x => new GetCompactMaterialsForAdminQueryItem(
                x.Id,
                x.Name,
                x.Article,
                x.MaterialSheetSizeId,
                x.MaterialManufacturerId,
                x.MaterialTypeId,
                x.MaterialThicknessId,
                x.CategoryId,
                x.Count,
                x.OrderByCol,
                x.Price,
                x.HideInLk,
                x.HideOnSite,
                x.HidePriceOnSite,
                x.Images
                    .Where(image => image.ImageType == MaterialImageTypeEnum.Thumbnail240
                        || image.ImageType == MaterialImageTypeEnum.Thumbnail480)
                    .OrderBy(image => image.ImageType == MaterialImageTypeEnum.Thumbnail240 ? 0 : 1)
                    .Select(image => (Guid?)image.Guid)
                    .FirstOrDefault(),
                x.ExternalLink,
                x.StroydvorEnabled,
                x.StroydvorProductId,
                x.StroydvorArticle,
                x.StroydvorPrice,
                x.StroydvorLastSyncAt,
                x.StroydvorInStock,
                x.StroydvorProductId.HasValue ? $"https://stroydvor21.ru/products/{x.StroydvorProductId.Value}" : null,
                x.Comments.OrderBy(comment => comment.Id)
                    .Select(comment => new MaterialCommentDto(comment.Id, comment.Text, comment.IsForClient)).ToArray(),
                null))
            .ToArrayAsync(ct);

        if (items.Length > 0)
        {
            var percentages = MaterialClientPricePercentages.From(await dbContext.PricePercentages.AsNoTracking()
                .Where(percentage => percentage.AppliesTo == PricePercentageTargetEnum.MATERIAL)
                .ToDictionaryAsync(percentage => percentage.Key, percentage => percentage.Value, ct));
            items = items.Select(material => material with
            {
                ClientPrices = percentages.GetPrices(material.P, material.Spp)
            }).ToArray();
        }

        var categories = await dbContext.MaterialCategories.AsNoTracking()
            .OrderBy(x => x.OrderByCol)
            .Select(x => new MaterialsForAdminCategoryFilterDto(x.Id, x.Name, x.HideOnSite, x.HideInLk))
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
            .Select(x => new MaterialsForAdminThicknessFilterDto(x.Id, x.Name, x.Value))
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

        return new GetCompactMaterialsForAdminQueryResult(items,
            new MaterialsForAdminFiltersDto(categories, manufacturers, materialTypes, thicknesses, sheetSizes));
    }
}