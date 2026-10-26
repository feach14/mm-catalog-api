// ReSharper disable NotAccessedPositionalProperty.Global

using Catalog.Database;
using Core.CQRS;

namespace Catalog.Api.Features.Materials.ForCalculate;

public sealed record GetMaterialsForCalculateQuery : IQuery<GetMaterialsForCalculateQueryResult>;

public sealed record GetMaterialsForCalculateQueryResult(
    [property: Description("Материалы")] GetMaterialsForCalculateQueryItem[] Items);

public sealed record GetMaterialsForCalculateQueryItem(
    [property: Description("Id")] int Id,
    [property: Description("Название")] string Name,
    [property: Description("Артикул")] string Article,
    [property: Description("Категория")] MaterialCategoryForCalculateDto Category);

public sealed record MaterialCategoryForCalculateDto(
    [property: Description("Id")] int Id,
    [property: Description("Название")] string Name,
    [property: Description("Порядок сортировки")] int OrderByCol);

public sealed class GetMaterialsForCalculateQueryHandler(CatalogDbContext dbContext)
    : IQueryHandler<GetMaterialsForCalculateQuery, GetMaterialsForCalculateQueryResult>
{
    public async Task<GetMaterialsForCalculateQueryResult> Handle(GetMaterialsForCalculateQuery query, CancellationToken ct)
    {
        var items = await dbContext.Materials.AsNoTracking()
            .OrderBy(x => x.OrderByCol).ThenBy(x => x.Id)
            .Select(x => new GetMaterialsForCalculateQueryItem(x.Id, x.Name, x.Article,
                new MaterialCategoryForCalculateDto(x.CategoryId, x.Category.Name, x.Category.OrderByCol)))
            .ToArrayAsync(ct);

        return new GetMaterialsForCalculateQueryResult(items);
    }
}