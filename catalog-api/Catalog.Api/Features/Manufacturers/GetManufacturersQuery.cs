using Catalog.Api.Features.Manufacturers.Dto;
using Catalog.Database;
using Core.CQRS;

namespace Catalog.Api.Features.Manufacturers;

public sealed record GetManufacturersQuery : IQuery<GetManufacturersQueryResult>;
public sealed record GetManufacturersQueryResult(ManufacturerDto[] Items);

public sealed class GetManufacturersQueryHandler(CatalogDbContext dbContext)
    : IQueryHandler<GetManufacturersQuery, GetManufacturersQueryResult>
{
    public async Task<GetManufacturersQueryResult> Handle(GetManufacturersQuery query, CancellationToken ct)
    {
        var items = await dbContext.MaterialManufacturers
            .OrderBy(x => x.OrderByCol)
            .Select(x => new ManufacturerDto(x.Id, x.Name, x.OrderByCol))
            .ToArrayAsync(ct);
        return new GetManufacturersQueryResult(items);
    }
}