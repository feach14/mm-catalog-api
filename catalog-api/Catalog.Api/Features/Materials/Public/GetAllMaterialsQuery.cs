// ReSharper disable NotAccessedPositionalProperty.Global

using Catalog.Api.Features.Materials.Dto;
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
    [property: Description("Размер материала")] PropertyDto SheetSize,
    [property: Description("Производитель")] PropertyDto Manufacturer,
    [property: Description("Тип материала")] PropertyDto MaterialType,
    [property: Description("Толщина материала")] PropertyDto Thickness,
    [property: Description("Категория")] PropertyDto Category,
    [property: Description("Количество")] int Count,
    [property: Description("Признак: Комментарий к материалу обязателен при оформлении заявки(расчета)")] bool CommentOnMaterialIsRequired,
    [property: Description("Порядковый номер записи (для сортировки)")] int OrderByCol);

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
                new PropertyDto(x.MaterialSheetSize.Id, x.MaterialSheetSize.Name),
                new PropertyDto(x.MaterialManufacturer.Id, x.MaterialManufacturer.Name),
                new PropertyDto(x.MaterialType.Id, x.MaterialType.Name),
                new PropertyDto(x.MaterialThickness.Id, x.MaterialThickness.Name),
                new PropertyDto(x.CategoryId, x.Category.Name),
                x.Count,
                x.CommentOnMaterialIsRequired,
                x.OrderByCol))
            .ToArrayAsync(ct);

        return new GetAllMaterialsQueryResult(materials);
    }
}