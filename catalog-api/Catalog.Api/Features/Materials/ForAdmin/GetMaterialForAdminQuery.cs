using Catalog.Api.Features.Materials.Dto;
using Catalog.Api.Features.Materials.ForAdmin.Dto;
using Catalog.Database;
using Catalog.Database.Enums;
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
    [property: Description("Количество кв. м. в плите")] decimal KvM,
    [property: Description("Количество метров по периметру плиты")] decimal PerimetrM,
    [property: Description("Использование материала в калькуляторе раскроя")] bool ApplicableToRaskroys,
    [property: Description("Использование материала в калькуляторе фасадов ПВХ")] bool ApplicableToPvhFacades,
    [property: Description("Использование материала в калькуляторе фасадов эмаль")] bool ApplicableToEmalFacades,
    [property: Description("Количество")] int Count,
    [property: Description("Порядковый номер записи (для сортировки)")] int OrderByCol,
    [property: Description("Признак: Комментарий к материалу обязателен при оформлении заявки(расчета)")] bool CommentOnMaterialIsRequired,
    [property: Description("Признак: Разрешено добавлять вторым(и более) элементом списка расчетов в заявке")] bool AllowSecondItemInOrder,
    [property: Description("Ссылка на внешний источник")] string? ExternalLink,
    [property: Description("Цена за материал у поставщика")] decimal Price,
    [property: Description("Единица измерения"), JsonConverter(typeof(JsonStringEnumConverter))] CountTypeEnum CountTypeEnum
);

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
                    Thumbnail240 = x.Images
                        .Where(image => image.ImageType == MaterialImageTypeEnum.Thumbnail240)
                        .Select(image => (Guid?)image.Guid)
                        .SingleOrDefault(),
                    Thumbnail480 = x.Images
                        .Where(image => image.ImageType == MaterialImageTypeEnum.Thumbnail480)
                        .Select(image => (Guid?)image.Guid)
                        .SingleOrDefault()
                },
                x.KvM,
                x.PerimetrM,
                x.ApplicableToRaskroys,
                x.ApplicableToPvhFacades,
                x.ApplicableToEmalFacades,
                x.Count,
                x.OrderByCol,
                x.CommentOnMaterialIsRequired,
                x.AllowSecondItemInOrder,
                x.ExternalLink,
                x.Price,
                x.CountTypeEnum))
            .SingleOrDefaultAsync(ct)
        ?? throw new BadHttpRequestException($"Материал с id={query.Id} не найден");
}