// ReSharper disable NotAccessedPositionalProperty.Global

using Catalog.Database;
using Core.CQRS;

namespace Catalog.Api.Features.MaterialSheetSizes.ForAdmin;

public sealed record GetMaterialSheetSizesQuery : IQuery<GetMaterialSheetSizesQueryResult>;

public sealed record GetMaterialSheetSizesQueryResult(
    [property: Description("Список размеров материалов")] GetMaterialSheetSizesQueryItem[] Items);

public sealed record GetMaterialSheetSizesQueryItem(
    [property: Description("Id размера материала")] int Id,
    [property: Description("Название размера материала")] string Name,
    [property: Description("Высота материала")] int Height,
    [property: Description("Ширина материала")] int Width,
    [property: Description("Показывать размер материала в фильтрах")] bool ShowInFilters,
    [property: Description("Порядковый номер записи (для сортировки)")] int OrderByCol,
    [property: Description("Количество материалов в наличии")] int MaterialsAnyCount,
    [property: Description("Количество материалов не в наличии")] int MaterialsNotAnyCount);

public sealed class GetMaterialSheetSizesQueryHandler(CatalogDbContext dbContext)
    : IQueryHandler<GetMaterialSheetSizesQuery, GetMaterialSheetSizesQueryResult>
{
    public async Task<GetMaterialSheetSizesQueryResult> Handle(GetMaterialSheetSizesQuery query, CancellationToken ct)
    {
        var items = await dbContext.MaterialSheetSizes
            .OrderBy(x => x.OrderByCol)
            .Select(x => new GetMaterialSheetSizesQueryItem(
                x.Id,
                x.Name,
                x.Height,
                x.Width,
                x.ShowInFilters,
                x.OrderByCol,
                x.Materials.Count(material => material.Count > 0),
                x.Materials.Count(material => material.Count < 1)))
            .ToArrayAsync(ct);
        return new GetMaterialSheetSizesQueryResult(items);
    }
}