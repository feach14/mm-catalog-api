// ReSharper disable NotAccessedPositionalProperty.Global

using Catalog.Database;
using Core.CQRS;

namespace Catalog.Api.Features.Manufacturers.ForAdmin;

public sealed record GetManufacturerQuery(int Id) : IQuery<GetManufacturerQueryResult>;

public sealed record GetManufacturerQueryResult(
    [property: Description("Id производителя")] int Id,
    [property: Description("Название производителя")] string Name,
    [property: Description("Порядковый номер записи (для сортировки)")] int OrderByCol);

public sealed class GetManufacturerQueryHandler(CatalogDbContext dbContext)
    : IQueryHandler<GetManufacturerQuery, GetManufacturerQueryResult>
{
    public async Task<GetManufacturerQueryResult> Handle(GetManufacturerQuery query, CancellationToken ct) =>
        await dbContext.MaterialManufacturers
            .Where(x => x.Id == query.Id)
            .Select(x => new GetManufacturerQueryResult(
                x.Id,
                x.Name,
                x.OrderByCol))
            .SingleOrDefaultAsync(ct)
        ?? throw new BadHttpRequestException($"Производитель с id={query.Id} не найден.");
}