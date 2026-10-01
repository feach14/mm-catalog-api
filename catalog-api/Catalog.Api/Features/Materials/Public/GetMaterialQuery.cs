using Catalog.Api.Features.Materials.Dto;
using Catalog.Database;
using Catalog.Database.Enums;
using Core.CQRS;

namespace Catalog.Api.Features.Materials.Public;

public sealed record GetMaterialQuery(int Id) : IQuery<GetMaterialQueryResult>;

public sealed record GetMaterialQueryResult(
    [property: Description("Id материала")] int Id,
    [property: Description("Название материала")] string Name,
    [property: Description("Артикул материала")] string Article,
    [property: Description("Категория")] PropertyDto Category,
    [property: Description("Размер материала")] PropertyDto SheetSize,
    [property: Description("Производитель")] PropertyDto Manufacturer,
    [property: Description("Тип материала")] PropertyDto MaterialType,
    [property: Description("Толщина материала")] PropertyDto Thickness,
    [property: Description("Изображения материала")] PublicMaterialImagesDto Images,
    [property: Description("Количество")] int Count,
    [property: Description("Признак: Комментарий к материалу обязателен при оформлении заявки(расчета)")] bool CommentOnMaterialIsRequired,
    [property: Description("Порядковый номер записи (для сортировки)")] int OrderByCol);

public class GetMaterialQueryHandler(CatalogDbContext dbContext) : IQueryHandler<GetMaterialQuery, GetMaterialQueryResult>
{
    public async Task<GetMaterialQueryResult> Handle(GetMaterialQuery query, CancellationToken ct)
        => await dbContext.Materials.AsNoTracking()
               .Where(x => x.Id == query.Id && !x.HideOnSite && !x.Category.HideOnSite)
               .Select(x => new GetMaterialQueryResult(
                   x.Id,
                   x.Name,
                   x.Article,
                   Category: new PropertyDto(x.CategoryId, x.Category.Name),
                   SheetSize: new PropertyDto(x.MaterialSheetSize.Id, x.MaterialSheetSize.Name),
                   Manufacturer: new PropertyDto(x.MaterialManufacturer.Id, x.MaterialManufacturer.Name),
                   MaterialType: new PropertyDto(x.MaterialType.Id, x.MaterialType.Name),
                   Thickness: new PropertyDto(x.MaterialThickness.Id, x.MaterialThickness.Name),
                   Images: new PublicMaterialImagesDto(
                       x.Images
                           .Where(image => image.ImageType == MaterialImageTypeEnum.Original)
                           .OrderBy(image => image.Id)
                           .Select(image => (Guid?)image.Guid)
                           .FirstOrDefault(),
                       x.Images
                           .Where(image => image.ImageType == MaterialImageTypeEnum.Thumbnail240)
                           .OrderBy(image => image.Id)
                           .Select(image => (Guid?)image.Guid)
                           .FirstOrDefault(),
                       x.Images
                           .Where(image => image.ImageType == MaterialImageTypeEnum.Thumbnail480)
                           .OrderBy(image => image.Id)
                           .Select(image => (Guid?)image.Guid)
                           .FirstOrDefault()),
                   x.Count,
                   x.CommentOnMaterialIsRequired,
                   x.OrderByCol))
               .SingleOrDefaultAsync(ct)
           ?? throw new BadHttpRequestException($"Материал с id={query.Id} не найден");
}