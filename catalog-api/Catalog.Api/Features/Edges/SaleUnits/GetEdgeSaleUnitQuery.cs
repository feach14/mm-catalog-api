using Catalog.Api.Features.Edges.SaleUnits.Dto;
using Catalog.Database;
using Core.CQRS;

namespace Catalog.Api.Features.Edges.SaleUnits;

public sealed record GetEdgeSaleUnitQuery(int Id) : IQuery<EdgeSaleUnitDto>;

public sealed class GetEdgeSaleUnitQueryHandler(CatalogDbContext dbContext)
    : IQueryHandler<GetEdgeSaleUnitQuery, EdgeSaleUnitDto>
{
    public async Task<EdgeSaleUnitDto> Handle(GetEdgeSaleUnitQuery query, CancellationToken ct) =>
        await dbContext.EdgeSaleUnits
            .AsNoTracking()
            .Where(x => x.Id == query.Id)
            .Select(x => new EdgeSaleUnitDto(
                x.Id,
                x.Name,
                x.Type,
                x.Length,
                x.Edges.Count(),
                x.Edges.Count(edge => edge.IsStockItem),
                x.Edges.Count(edge => !edge.IsStockItem)))
            .SingleOrDefaultAsync(ct)
        ?? throw new BadHttpRequestException("Запись единицы продажи кромок не найдена.");
}