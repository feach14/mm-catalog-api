using Catalog.Api.Features.Materials.Dto;
using Catalog.Api.Features.Materials.ForAdmin.Dto;
using Catalog.Api.Features.Materials.Helpers;
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
    [property: Description("Количество")] int Count,
    [property: Description("Порядковый номер записи (для сортировки)")] int OrderByCol,
    [property: Description("Признак: Комментарий к материалу обязателен при оформлении заявки(расчета)")] bool CommentOnMaterialIsRequired,
    [property: Description("Признак: Разрешено добавлять вторым(и более) элементом списка расчетов в заявке")] bool AllowSecondItemInOrder,
    [property: Description("Ссылка на внешний источник")] string? ExternalLink,
    [property: Description("Цена за материал у поставщика")] decimal Price,
    [property: Description("Скрыть в ЛК")] bool HideInLk,
    [property: Description("Признак: скрыть материал на сайте")] bool HideOnSite,
    [property: Description("Признак: не показывать цену материала на сайте")] bool HidePriceOnSite,
    [property: Description("Единица измерения"), JsonConverter(typeof(StrictStringEnumConverter))] CountTypeEnum CountTypeEnum,
    [property: Description("Связь со Стройдвором включена")] bool StroydvorEnabled,
    [property: Description("Id товара Стройдвора")] int? StroydvorProductId,
    [property: Description("Артикул Стройдвора")] string? StroydvorArticle,
    [property: Description("Цена Стройдвора за лист")] decimal StroydvorPrice,
    [property: Description("Дата успешной синхронизации со Стройдвором")] DateTimeOffset? StroydvorLastSyncAt,
    [property: Description("В наличии в Стройдворе")] bool StroydvorInStock,
    [property: Description("Материал в архиве")] bool IsArchived,
    [property: Description("Id исходного материала до переноса в архив")] int? OriginalMaterialId,
    [property: Description("Комментарии")] MaterialCommentDto[] Comments,
    [property: Description("Цены для клиентов")] MaterialClientPricesDto? ClientPrices)
{
    [Description("Техническая ссылка на товар Стройдвора")]
    public string? StroydvorUrl => StroydvorProductId is { } id ? $"https://stroydvor21.ru/products/{id}" : null;
}

public class GetMaterialForAdminQueryHandler(CatalogDbContext dbContext) : IQueryHandler<GetMaterialForAdminQuery, GetMaterialForAdminQueryResult>
{
    public async Task<GetMaterialForAdminQueryResult> Handle(GetMaterialForAdminQuery query, CancellationToken ct)
    {
        var material = await dbContext.Materials.AsNoTracking()
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
                x.Count,
                x.OrderByCol,
                x.CommentOnMaterialIsRequired,
                x.AllowSecondItemInOrder,
                x.ExternalLink,
                x.Price,
                x.HideInLk,
                x.HideOnSite,
                x.HidePriceOnSite,
                x.CountTypeEnum,
                x.StroydvorEnabled,
                x.StroydvorProductId,
                x.StroydvorArticle,
                x.StroydvorPrice,
                x.StroydvorLastSyncAt,
                x.StroydvorInStock,
                false,
                null,
                x.Comments.OrderBy(comment => comment.Id)
                    .Select(comment => new MaterialCommentDto(comment.Id, comment.Text, comment.IsForClient)).ToArray(),
                null))
            .SingleOrDefaultAsync(ct)
            ?? await GetArchivedMaterial(query.Id, ct);

        var percentages = MaterialClientPricePercentages.From(await dbContext.PricePercentages.AsNoTracking()
                .Where(percentage => percentage.AppliesTo == PricePercentageTargetEnum.MATERIAL)
                .ToDictionaryAsync(percentage => percentage.Key, percentage => percentage.Value, ct));
        return material with { ClientPrices = percentages.GetPrices(material.Price, material.StroydvorPrice) };
    }

    private async Task<GetMaterialForAdminQueryResult> GetArchivedMaterial(int id, CancellationToken ct) =>
        await dbContext.ArchivedMaterials.AsNoTracking()
            .Where(x => x.Id == id)
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
                x.Count,
                x.OrderByCol,
                x.CommentOnMaterialIsRequired,
                x.AllowSecondItemInOrder,
                x.ExternalLink,
                x.Price,
                x.HideInLk,
                x.HideOnSite,
                x.HidePriceOnSite,
                x.CountTypeEnum,
                x.StroydvorEnabled,
                x.StroydvorProductId,
                x.StroydvorArticle,
                x.StroydvorPrice,
                x.StroydvorLastSyncAt,
                x.StroydvorInStock,
                true,
                x.OriginalMaterialId,
                x.Comments.OrderBy(comment => comment.Id)
                    .Select(comment => new MaterialCommentDto(comment.Id, comment.Text, comment.IsForClient)).ToArray(),
                null))
            .SingleOrDefaultAsync(ct)
            ?? throw new BadHttpRequestException($"Материал с id={id} не найден");
}