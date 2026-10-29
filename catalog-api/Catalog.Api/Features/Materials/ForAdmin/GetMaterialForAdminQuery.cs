using Catalog.Api.Features.Materials.Dto;
using Catalog.Api.Features.Materials.ForAdmin.Dto;
using Catalog.Database;
using Catalog.Database.Enums;
using Core.BaseEnums;
using Core.CQRS;

namespace Catalog.Api.Features.Materials.ForAdmin;

public sealed record GetMaterialForAdminQuery(int Id) : IQuery<GetMaterialForAdminQueryResult>;

public sealed record GetMaterialForAdminQueryResult(
    [property: Description("Id материала")] int Id,
    [property: Description("Название материала")] string Name,
    [property: Description("Артикул материала")] string Article,
    [property: Description("Категория")] PropertyDto Category,
    [property: Description("Размер материала")] PropertyDto SheetSize,
    [property: Description("Производитель")] PropertyDto Manufacturer,
    [property: Description("Тип материала")] PropertyDto MaterialType,
    [property: Description("Толщина материала")] PropertyDto Thickness,
    [property: Description("Оригинал и миниатюры материала")] MaterialImagesDto Images,
    [property: Description("Использование материала в заявке на распил")] bool ApplicableToCutting,
    [property: Description("Использование материала в заявке на фасады ПВХ")] bool ApplicableToPvhFacades,
    [property: Description("Использование материала в заявке на фасады эмаль")] bool ApplicableToEnamelFacades,
    [property: Description("Количество")] int Count,
    [property: Description("Порядковый номер записи (для сортировки)")] int OrderByCol,
    [property: Description("Признак: Комментарий к материалу обязателен при оформлении заявки(расчета)")] bool CommentOnMaterialIsRequired,
    [property: Description("Признак: Разрешено добавлять вторым(и более) элементом списка расчетов в заявке")] bool AllowSecondItemInOrder,
    [property: Description("Ссылка на внешний источник")] string? ExternalLink,
    [property: Description("Цена за материал у поставщика")] decimal Price,
    [property: Description("Признак: скрыть материал на сайте")] bool HideOnSite,
    [property: Description("Признак: не показывать цену материала на сайте")] bool HidePriceOnSite,
    [property: Description("Единица измерения"), JsonConverter(typeof(StrictStringEnumConverter))] CountTypeEnum CountTypeEnum,
    [property: Description("Id товара Стройдвора")] int? StroydvorProductId,
    [property: Description("Артикул Стройдвора")] string? StroydvorArticle,
    [property: Description("Цена Стройдвора за лист")] decimal StroydvorPrice,
    [property: Description("Дата успешной синхронизации со Стройдвором")] DateTimeOffset? StroydvorLastSyncAt,
    [property: Description("В наличии в Стройдворе")] bool StroydvorInStock)
{
    [Description("Техническая ссылка на товар Стройдвора")]
    public string? StroydvorUrl => StroydvorProductId is { } id ? $"https://stroydvor21.ru/products/{id}" : null;
}

public class GetMaterialForAdminQueryHandler(CatalogDbContext dbContext) : IQueryHandler<GetMaterialForAdminQuery, GetMaterialForAdminQueryResult>
{
    public async Task<GetMaterialForAdminQueryResult> Handle(GetMaterialForAdminQuery query, CancellationToken ct) =>
        await dbContext.Materials.AsNoTracking()
            .Where(x => x.Id == query.Id)
            .Select(x => new GetMaterialForAdminQueryResult(
                x.Id,
                x.Name,
                x.Article,
                new PropertyDto(x.CategoryId, x.Category.Name),
                new PropertyDto(x.MaterialSheetSize.Id, x.MaterialSheetSize.Name),
                new PropertyDto(x.MaterialManufacturer.Id, x.MaterialManufacturer.Name),
                new PropertyDto(x.MaterialType.Id, x.MaterialType.Name),
                new PropertyDto(x.MaterialThickness.Id, x.MaterialThickness.Name),
                new MaterialImagesDto
                {
                    Original = x.Images
                        .Where(image => image.ImageType == MaterialImageTypeEnum.Original)
                        .Select(image => (Guid?)image.Guid)
                        .SingleOrDefault(),
                    OriginalMetadata = x.Images
                        .Where(image => image.ImageType == MaterialImageTypeEnum.Original)
                        .Select(image => new MaterialImageMetadataDto
                        {
                            ContentType = image.ContentType,
                            Size = image.Data.Length
                        })
                        .SingleOrDefault(),
                    Thumbnail240 = x.Images
                        .Where(image => image.ImageType == MaterialImageTypeEnum.Thumbnail240)
                        .Select(image => (Guid?)image.Guid)
                        .SingleOrDefault(),
                    Thumbnail240Metadata = x.Images
                        .Where(image => image.ImageType == MaterialImageTypeEnum.Thumbnail240)
                        .Select(image => new MaterialImageMetadataDto
                        {
                            ContentType = image.ContentType,
                            Size = image.Data.Length
                        })
                        .SingleOrDefault(),
                    Thumbnail480 = x.Images
                        .Where(image => image.ImageType == MaterialImageTypeEnum.Thumbnail480)
                        .Select(image => (Guid?)image.Guid)
                        .SingleOrDefault(),
                    Thumbnail480Metadata = x.Images
                        .Where(image => image.ImageType == MaterialImageTypeEnum.Thumbnail480)
                        .Select(image => new MaterialImageMetadataDto
                        {
                            ContentType = image.ContentType,
                            Size = image.Data.Length
                        })
                        .SingleOrDefault()
                },
                x.ApplicableToCutting,
                x.ApplicableToPvhFacades,
                x.ApplicableToEnamelFacades,
                x.Count,
                x.OrderByCol,
                x.CommentOnMaterialIsRequired,
                x.AllowSecondItemInOrder,
                x.ExternalLink,
                x.Price,
                x.HideOnSite,
                x.HidePriceOnSite,
                x.CountTypeEnum,
                x.StroydvorProductId,
                x.StroydvorArticle,
                x.StroydvorPrice,
                x.StroydvorLastSyncAt,
                x.StroydvorInStock))
            .SingleOrDefaultAsync(ct)
        ?? throw new BadHttpRequestException($"Материал с id={query.Id} не найден");
}