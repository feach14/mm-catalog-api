// ReSharper disable NotAccessedPositionalProperty.Global

using Catalog.Api.Features.Materials.Dto;
using Catalog.Api.Features.MaterialThicknesses.Dto;
using Catalog.Database;
using Core.CQRS;

namespace Catalog.Api.Features.Materials.Public;

public sealed record GetAllMaterialsQuery : IQuery<GetAllMaterialsQueryResult>;

public sealed record GetAllMaterialsQueryResult(
    [property: Description("Список материалов")] GetAllMaterialsQueryItemResult[] Items);

public sealed record GetAllMaterialsQueryItemResult(
    [property: Description("Id материала")] int Id,
    [property: Description("Название материала")] string Name,
    [property: Description("Артикул материала")] string Article,
    [property: Description("Размер материала")] MaterialSheetSizeDto SheetSize,
    [property: Description("Производитель")] MaterialManufacturerDto Manufacturer,
    [property: Description("Толщина материала")] MaterialThicknessDto Thickness,
    [property: Description("Категория")] CategoryDto Category,
    [property: Description("Количество")] int Count,
    [property: Description("Признак: Комментарий к материалу обязателен при оформлении заявки(расчета)")] bool CommentOnMaterialIsRequired,
    [property: Description("Порядковый номер записи (для сортировки)")] int OrderByCol);

public record MaterialCategoryDto(
    [property: Description("Id категории")] int Id,
    [property: Description("Название категории")] string Name,
    [property: Description("Порядковый номер для сортировки")] int OrderByCol);

public class GetAllMaterialsQueryHandler(CatalogDbContext dbContext) : IQueryHandler<GetAllMaterialsQuery, GetAllMaterialsQueryResult>
{
    public async Task<GetAllMaterialsQueryResult> Handle(GetAllMaterialsQuery query, CancellationToken ct)
    {
        var materials = await dbContext.Materials.AsNoTracking()
            .OrderBy(x => x.Category.OrderByCol)
            .ThenBy(x => x.OrderByCol)
            .Select(x => new GetAllMaterialsQueryItemResult(
                x.Id,
                x.Name,
                x.Article,
                new MaterialSheetSizeDto(x.MaterialSheetSize.Id, x.MaterialSheetSize.Name, x.MaterialSheetSize.Height, x.MaterialSheetSize.Width),
                new MaterialManufacturerDto(x.MaterialManufacturer.Id, x.MaterialManufacturer.Name),
                new MaterialThicknessDto(x.MaterialThickness.Id, x.MaterialThickness.Name, x.MaterialThickness.Value),
                new CategoryDto(x.CategoryId, x.Category.Name),
                x.Count,
                x.CommentOnMaterialIsRequired,
                x.OrderByCol))
            .ToArrayAsync(ct);

        return new GetAllMaterialsQueryResult(materials);
    }
}