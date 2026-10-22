using Catalog.Api.Features.Edges.Manufacturers.Dto;
using Catalog.Database;
using Core.CQRS;

namespace Catalog.Api.Features.Edges.Manufacturers;

public sealed record GetEdgeManufacturerQuery(int Id) : IQuery<EdgeManufacturerDto>;

public sealed class GetEdgeManufacturerQueryHandler(CatalogDbContext dbContext)
    : IQueryHandler<GetEdgeManufacturerQuery, EdgeManufacturerDto>
{
    public async Task<EdgeManufacturerDto> Handle(GetEdgeManufacturerQuery query, CancellationToken ct) =>
        await dbContext.EdgeManufacturers
            .AsNoTracking()
            .Where(x => x.Id == query.Id)
            .Select(x => new EdgeManufacturerDto(
                x.Id,
                x.Name,
                x.Edges.Count(),
                x.Edges.Count(edge => edge.IsStockItem),
                x.Edges.Count(edge => !edge.IsStockItem)))
            .SingleOrDefaultAsync(ct)
        ?? throw new BadHttpRequestException("Запись производителя кромок не найдена.");
}