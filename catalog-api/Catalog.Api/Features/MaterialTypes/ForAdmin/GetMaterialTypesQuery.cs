// ReSharper disable NotAccessedPositionalProperty.Global

using Catalog.Database;
using Core.CQRS;

namespace Catalog.Api.Features.MaterialTypes.ForAdmin;

public sealed record GetMaterialTypesQuery : IQuery<GetMaterialTypesQueryResult>;

public sealed record GetMaterialTypesQueryResult(
    [property: Description("Список типов материалов")] GetMaterialTypesQueryItem[] Items);

public sealed record GetMaterialTypesQueryItem(
    [property: Description("Id типа материала")] int Id,
    [property: Description("Название типа материала")] string Name,
    [property: Description("Порядковый номер записи (для сортировки)")] int OrderByCol,
    [property: Description("Количество материалов в наличии")] int MaterialsAnyCount,
    [property: Description("Количество материалов не в наличии")] int MaterialsNotAnyCount);

public sealed class GetMaterialTypesQueryHandler(CatalogDbContext dbContext)
    : IQueryHandler<GetMaterialTypesQuery, GetMaterialTypesQueryResult>
{
    public async Task<GetMaterialTypesQueryResult> Handle(GetMaterialTypesQuery query, CancellationToken ct)
    {
        var items = await dbContext.MaterialTypes.AsNoTracking()
            .OrderBy(x => x.OrderByCol)
            .Select(x => new GetMaterialTypesQueryItem(
                x.Id,
                x.Name,
                x.OrderByCol,
                x.Materials.Count(material => material.Count > 0),
                x.Materials.Count(material => material.Count < 1)))
            .ToArrayAsync(ct);
        return new GetMaterialTypesQueryResult(items);
    }
}