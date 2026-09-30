// ReSharper disable NotAccessedPositionalProperty.Global

using Catalog.Database;
using Core.CQRS;

namespace Catalog.Api.Features.MaterialTypes.ForAdmin;

public sealed record GetMaterialTypeQuery(int Id) : IQuery<GetMaterialTypeQueryResult>;

public sealed record GetMaterialTypeQueryResult(
    [property: Description("Id типа материала")] int Id,
    [property: Description("Название типа материала")] string Name,
    [property: Description("Порядковый номер записи (для сортировки)")] int OrderByCol);

public sealed class GetMaterialTypeQueryHandler(CatalogDbContext dbContext)
    : IQueryHandler<GetMaterialTypeQuery, GetMaterialTypeQueryResult>
{
    public async Task<GetMaterialTypeQueryResult> Handle(GetMaterialTypeQuery query, CancellationToken ct) =>
        await dbContext.MaterialTypes.AsNoTracking()
            .Where(x => x.Id == query.Id)
            .Select(x => new GetMaterialTypeQueryResult(
                x.Id,
                x.Name,
                x.OrderByCol
            ))
            .SingleOrDefaultAsync(ct)
        ?? throw new BadHttpRequestException($"Тип материала с id={query.Id} не найден.");
}