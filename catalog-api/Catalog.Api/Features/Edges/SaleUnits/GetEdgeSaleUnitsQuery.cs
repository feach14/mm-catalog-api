using Catalog.Api.Features.Edges.SaleUnits.Dto;
using Catalog.Database;
using Core.CQRS;

namespace Catalog.Api.Features.Edges.SaleUnits;

public sealed record GetEdgeSaleUnitsQuery : IQuery<GetEdgeSaleUnitsQueryResult>;

[Description("Список единиц продажи кромок")]
public sealed record GetEdgeSaleUnitsQueryResult(
    [property: Description("Единицы продажи кромок")] EdgeSaleUnitDto[] Items);

public sealed class GetEdgeSaleUnitsQueryHandler(CatalogDbContext dbContext)
    : IQueryHandler<GetEdgeSaleUnitsQuery, GetEdgeSaleUnitsQueryResult>
{
    public async Task<GetEdgeSaleUnitsQueryResult> Handle(GetEdgeSaleUnitsQuery query, CancellationToken ct)
    {
        EdgeSaleUnitDto[] items = await dbContext.EdgeSaleUnits
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .ThenBy(x => x.Id)
            .Select(x => new EdgeSaleUnitDto(
                x.Id,
                x.Name,
                x.SaleUnitType,
                x.Length,
                x.Edges.Count(),
                x.Edges.Count(edge => edge.IsStockItem),
                x.Edges.Count(edge => !edge.IsStockItem)))
            .ToArrayAsync(ct);
        return new GetEdgeSaleUnitsQueryResult(items);
    }
}