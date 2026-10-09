// ReSharper disable NotAccessedPositionalProperty.Global

using Catalog.Api.Features.Materials.Dto;
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
    [property: Description("Категория")] PropertyDto Category);

public sealed class GetMaterialsForCalculateQueryHandler(CatalogDbContext dbContext)
    : IQueryHandler<GetMaterialsForCalculateQuery, GetMaterialsForCalculateQueryResult>
{
    public async Task<GetMaterialsForCalculateQueryResult> Handle(GetMaterialsForCalculateQuery query, CancellationToken ct)
    {
        var items = await dbContext.Materials.AsNoTracking()
            .OrderBy(x => x.Category.OrderByCol)
            .ThenBy(x => x.OrderByCol)
            .Select(x => new GetMaterialsForCalculateQueryItem(x.Id, x.Name, x.Article,
                new PropertyDto(x.CategoryId, x.Category.Name)))
            .ToArrayAsync(ct);

        return new GetMaterialsForCalculateQueryResult(items);
    }
}