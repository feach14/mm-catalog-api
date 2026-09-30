// ReSharper disable NotAccessedPositionalProperty.Global

using Catalog.Database;
using Core.CQRS;

namespace Catalog.Api.Features.MaterialTypes.Public;

public sealed record GetPublicMaterialTypesQuery : IQuery<GetPublicMaterialTypesQueryResult>;

public sealed record GetPublicMaterialTypesQueryResult(
    [property: Description("Полный список типов материалов в порядке каталога")] PublicMaterialTypeListItem[] Items);

public sealed record PublicMaterialTypeListItem(
    [property: Description("Id типа материала")] int Id,
    [property: Description("Название типа материала")] string Name,
    [property: Description("Порядковый номер для сортировки")] int OrderByCol);

public sealed class GetPublicMaterialTypesQueryHandler(CatalogDbContext dbContext)
    : IQueryHandler<GetPublicMaterialTypesQuery, GetPublicMaterialTypesQueryResult>
{
    public async Task<GetPublicMaterialTypesQueryResult> Handle(GetPublicMaterialTypesQuery query, CancellationToken ct)
    {
        var items = await dbContext.MaterialTypes.AsNoTracking()
            .OrderBy(x => x.OrderByCol)
            .Select(x => new PublicMaterialTypeListItem(x.Id, x.Name, x.OrderByCol))
            .ToArrayAsync(ct);

        return new GetPublicMaterialTypesQueryResult(items);
    }
}