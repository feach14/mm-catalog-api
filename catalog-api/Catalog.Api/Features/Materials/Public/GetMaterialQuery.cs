using Catalog.Api.Features.Materials.Dto;
using Catalog.Api.Features.MaterialThicknesses.Dto;
using Catalog.Database;
using Catalog.Database.Enums;
using Core.CQRS;

namespace Catalog.Api.Features.Materials.Public;

public sealed record GetMaterialQuery(int Id) : IQuery<GetMaterialQueryResult>;

public sealed record GetMaterialQueryResult(
    [property: Description("Id материала")] int Id,
    [property: Description("Название материала")] string Name,
    [property: Description("Артикул материала")] string Article,
    [property: Description("Размер материала")] MaterialSheetSizeDto SheetSize,
    [property: Description("Производитель")] MaterialManufacturerDto Manufacturer,
    [property: Description("Толщина материала")] MaterialThicknessDto Thickness,
    [property: Description("Изображения материала")] PublicMaterialImagesDto Images,
    [property: Description("Категория")] CategoryDto Category,
    [property: Description("Количество")] int Count,
    [property: Description("Признак: Комментарий к материалу обязателен при оформлении заявки(расчета)")] bool CommentOnMaterialIsRequired,
    [property: Description("Порядковый номер записи (для сортировки)")] int OrderByCol);

public class GetMaterialQueryHandler(CatalogDbContext dbContext) : IQueryHandler<GetMaterialQuery, GetMaterialQueryResult>
{
    public async Task<GetMaterialQueryResult> Handle(GetMaterialQuery query, CancellationToken ct)
        => await dbContext.Materials.AsNoTracking()
               .Where(x => x.Id == query.Id)
               .Select(x => new GetMaterialQueryResult(
                   x.Id,
                   x.Name,
                   x.Article,
                   new MaterialSheetSizeDto(x.MaterialSheetSize.Id, x.MaterialSheetSize.Name, x.MaterialSheetSize.Height, x.MaterialSheetSize.Width),
                   new MaterialManufacturerDto(x.MaterialManufacturer.Id, x.MaterialManufacturer.Name),
                   new MaterialThicknessDto(x.MaterialThickness.Id, x.MaterialThickness.Name, x.MaterialThickness.Value),
                   new PublicMaterialImagesDto(
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
                   new CategoryDto(x.CategoryId, x.Category.Name),
                   x.Count,
                   x.CommentOnMaterialIsRequired,
                   x.OrderByCol))
               .SingleOrDefaultAsync(ct)
           ?? throw new BadHttpRequestException($"Материал с id={query.Id} не найден");
}