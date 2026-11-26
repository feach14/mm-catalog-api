using Catalog.Api.Features.Edges.Suppliers.Dto;
using Catalog.Database;
using Core.CQRS;

namespace Catalog.Api.Features.Edges.Suppliers;

public sealed record GetEdgeSupplierQuery(int Id) : IQuery<EdgeSupplierDto>;

public sealed class GetEdgeSupplierQueryHandler(CatalogDbContext dbContext)
    : IQueryHandler<GetEdgeSupplierQuery, EdgeSupplierDto>
{
    public async Task<EdgeSupplierDto> Handle(GetEdgeSupplierQuery query, CancellationToken ct) =>
        await dbContext.EdgeSuppliers
            .AsNoTracking()
            .Where(x => x.Id == query.Id)
            .Select(x => new EdgeSupplierDto(
                x.Id,
                x.Name,
                x.IncludeInEdgeName,
                x.OrderByCol,
                x.Edges.Count(),
                x.Edges.Count(edge => edge.IsStockItem),
                x.Edges.Count(edge => !edge.IsStockItem)))
            .SingleOrDefaultAsync(ct)
        ?? throw new BadHttpRequestException("Поставщик кромок не найден.");
}