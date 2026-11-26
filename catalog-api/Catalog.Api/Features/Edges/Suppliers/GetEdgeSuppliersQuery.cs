using Catalog.Api.Features.Edges.Suppliers.Dto;
using Catalog.Database;
using Core.CQRS;

namespace Catalog.Api.Features.Edges.Suppliers;

public sealed record GetEdgeSuppliersQuery : IQuery<GetEdgeSuppliersQueryResult>;

[Description("Список поставщиков кромок")]
public sealed record GetEdgeSuppliersQueryResult(
    [property: Description("Поставщики кромок")] EdgeSupplierDto[] Items);

public sealed class GetEdgeSuppliersQueryHandler(CatalogDbContext dbContext)
    : IQueryHandler<GetEdgeSuppliersQuery, GetEdgeSuppliersQueryResult>
{
    public async Task<GetEdgeSuppliersQueryResult> Handle(GetEdgeSuppliersQuery query, CancellationToken ct)
    {
        EdgeSupplierDto[] items = await dbContext.EdgeSuppliers
            .AsNoTracking()
            .OrderBy(x => x.OrderByCol)
            .Select(x => new EdgeSupplierDto(
                x.Id,
                x.Name,
                x.IncludeInEdgeName,
                x.OrderByCol,
                x.Edges.Count(),
                x.Edges.Count(edge => edge.IsStockItem),
                x.Edges.Count(edge => !edge.IsStockItem)))
            .ToArrayAsync(ct);
        return new GetEdgeSuppliersQueryResult(items);
    }
}