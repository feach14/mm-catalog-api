using Catalog.Database;
using Core.CQRS;

namespace Catalog.Api.Features.MaterialSheetSizes.ForAdmin;

public sealed record GetMaterialSheetSizeQuery(int Id) : IQuery<GetMaterialSheetSizeQueryResult>;

public sealed record GetMaterialSheetSizeQueryResult(
    [property: Description("Id размера материала")] int Id,
    [property: Description("Название размера материала")] string Name,
    [property: Description("Высота материала")] int Height,
    [property: Description("Ширина материала")] int Width,
    [property: Description("Показывать размер материала в фильтрах")] bool ShowInFilters,
    [property: Description("Порядковый номер записи (для сортировки)")] int OrderByCol);

public sealed class GetMaterialSheetSizeQueryHandler(CatalogDbContext dbContext)
    : IQueryHandler<GetMaterialSheetSizeQuery, GetMaterialSheetSizeQueryResult>
{
    public async Task<GetMaterialSheetSizeQueryResult> Handle(GetMaterialSheetSizeQuery query, CancellationToken ct) =>
        await dbContext.MaterialSheetSizes
            .Where(x => x.Id == query.Id)
            .Select(x => new GetMaterialSheetSizeQueryResult(
                x.Id,
                x.Name,
                x.Height,
                x.Width,
                x.ShowInFilters,
                x.OrderByCol))
            .SingleOrDefaultAsync(ct)
        ?? throw new BadHttpRequestException($"Размер материала с id={query.Id} не найден.");
}