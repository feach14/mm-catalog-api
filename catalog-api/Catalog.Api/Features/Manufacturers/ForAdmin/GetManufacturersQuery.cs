// ReSharper disable NotAccessedPositionalProperty.Global

using Catalog.Database;
using Core.CQRS;

namespace Catalog.Api.Features.Manufacturers.ForAdmin;

public sealed record GetManufacturersQuery : IQuery<GetManufacturersQueryResult>;

public sealed record GetManufacturersQueryResult(
    [property: Description("Список производителей")] GetManufacturersQueryItem[] Items);

public sealed record GetManufacturersQueryItem(
    [property: Description("Id производителя")] int Id,
    [property: Description("Название производителя")] string Name,
    [property: Description("Порядковый номер записи (для сортировки)")] int OrderByCol,
    [property: Description("Количество материалов в наличии")] int MaterialsAnyCount,
    [property: Description("Количество материалов не в наличии")] int MaterialsNotAnyCount);

public sealed class GetManufacturersQueryHandler(CatalogDbContext dbContext)
    : IQueryHandler<GetManufacturersQuery, GetManufacturersQueryResult>
{
    public async Task<GetManufacturersQueryResult> Handle(GetManufacturersQuery query, CancellationToken ct)
    {
        var items = await dbContext.MaterialManufacturers
            .OrderBy(x => x.OrderByCol)
            .Select(x => new GetManufacturersQueryItem(
                x.Id,
                x.Name,
                x.OrderByCol,
                x.Materials.Count(material => material.Count > 0),
                x.Materials.Count(material => material.Count < 1)))
            .ToArrayAsync(ct);
        return new GetManufacturersQueryResult(items);
    }
}