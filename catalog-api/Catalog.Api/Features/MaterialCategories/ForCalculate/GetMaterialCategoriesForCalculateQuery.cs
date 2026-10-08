// ReSharper disable NotAccessedPositionalProperty.Global

using Catalog.Database;
using Core.CQRS;

namespace Catalog.Api.Features.MaterialCategories.ForCalculate;

public sealed record GetMaterialCategoriesForCalculateQuery : IQuery<GetMaterialCategoriesForCalculateQueryResult>;

public sealed record GetMaterialCategoriesForCalculateQueryResult(
    [property: Description("Список категорий")] GetMaterialCategoriesForCalculateQueryItem[] Items);

public sealed record GetMaterialCategoriesForCalculateQueryItem(
    [property: Description("Id")] int Id,
    [property: Description("Название")] string Name);

public sealed class GetMaterialCategoriesForCalculateQueryHandler(CatalogDbContext dbContext)
    : IQueryHandler<GetMaterialCategoriesForCalculateQuery, GetMaterialCategoriesForCalculateQueryResult>
{
    public async Task<GetMaterialCategoriesForCalculateQueryResult> Handle(GetMaterialCategoriesForCalculateQuery query, CancellationToken ct)
    {
        var items = await dbContext.MaterialCategories.AsNoTracking()
            .OrderBy(x => x.OrderByCol)
            .ThenBy(x => x.Id)
            .Select(x => new GetMaterialCategoriesForCalculateQueryItem(x.Id, x.Name))
            .ToArrayAsync(ct);

        return new GetMaterialCategoriesForCalculateQueryResult(items);
    }
}