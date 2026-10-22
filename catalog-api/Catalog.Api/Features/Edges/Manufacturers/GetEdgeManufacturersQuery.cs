using Catalog.Api.Features.Edges.Manufacturers.Dto;
using Catalog.Database;
using Core.CQRS;

namespace Catalog.Api.Features.Edges.Manufacturers;

public sealed record GetEdgeManufacturersQuery : IQuery<GetEdgeManufacturersQueryResult>;

[Description("Список производителей кромок")]
public sealed record GetEdgeManufacturersQueryResult(
    [property: Description("Производители кромок")] EdgeManufacturerDto[] Items);

public sealed class GetEdgeManufacturersQueryHandler(CatalogDbContext dbContext)
    : IQueryHandler<GetEdgeManufacturersQuery, GetEdgeManufacturersQueryResult>
{
    public async Task<GetEdgeManufacturersQueryResult> Handle(GetEdgeManufacturersQuery query, CancellationToken ct)
    {
        EdgeManufacturerDto[] items = await dbContext.EdgeManufacturers
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .ThenBy(x => x.Id)
            .Select(x => new EdgeManufacturerDto(
                x.Id,
                x.Name,
                x.Edges.Count(),
                x.Edges.Count(edge => edge.IsStockItem),
                x.Edges.Count(edge => !edge.IsStockItem)))
            .ToArrayAsync(ct);
        return new GetEdgeManufacturersQueryResult(items);
    }
}