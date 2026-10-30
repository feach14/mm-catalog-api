// ReSharper disable NotAccessedPositionalProperty.Global

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
    [property: Description("Id товара Стройдвора")] int? Spid,
    [property: Description("Артикул Стройдвора")] string? Spa,
    [property: Description("Цена Стройдвора за лист")] decimal Spp,
    [property: Description("Дата успешной синхронизации со Стройдвором")] DateTimeOffset? Spls,
    [property: Description("В наличии в Стройдворе")] bool Spis,
    [property: Description("Техническая ссылка на товар Стройдвора")] string? Spurl);

public sealed class GetCompactMaterialsForAdminQueryHandler(
    IQueryHandler<GetAllMaterialsForAdminQuery, GetAllMaterialsForAdminQueryResult> materialsHandler,
    CatalogDbContext dbContext)
    : IQueryHandler<GetCompactMaterialsForAdminQuery, GetCompactMaterialsForAdminQueryResult>
{
    public async Task<GetCompactMaterialsForAdminQueryResult> Handle(GetCompactMaterialsForAdminQuery query, CancellationToken ct)
    {
        var materials = await materialsHandler.Handle(new GetAllMaterialsForAdminQuery(), ct);
        var thumbnails = await dbContext.MaterialImages.AsNoTracking()
            .Where(image => image.ImageType == MaterialImageTypeEnum.Thumbnail240
                || image.ImageType == MaterialImageTypeEnum.Thumbnail480)
            .OrderBy(image => image.ImageType == MaterialImageTypeEnum.Thumbnail240 ? 0 : 1)
            .Select(image => new { image.MaterialId, Guid = (Guid?)image.Guid })
            .ToArrayAsync(ct);
        var previewByMaterial = thumbnails.GroupBy(image => image.MaterialId)
            .ToDictionary(group => group.Key, group => group.First().Guid);
        var items = materials.Items.Select(x => new GetCompactMaterialsForAdminQueryItem(
            x.Id,
            x.Name,
            x.Article,
            x.SheetSize.Id,
            x.Manufacturer.Id,
            x.MaterialType.Id,
            x.Thickness.Id,
            x.Category.Id,
            x.Count,
            x.OrderByCol,
            x.Price,
            x.HideInLk,
            x.HideOnSite,
            x.HidePriceOnSite,
            previewByMaterial.GetValueOrDefault(x.Id),
            x.ExternalLink,
            x.StroydvorProductId,
            x.StroydvorArticle,
            x.StroydvorPrice,
            x.StroydvorLastSyncAt,
            x.StroydvorInStock,
            x.StroydvorUrl)).ToArray();

        return new GetCompactMaterialsForAdminQueryResult(items, materials.Filters);
    }
}