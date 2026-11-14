using Catalog.Api.Features.Edges.Sizes.Dto;
using Catalog.Database;
using Core.CQRS;

namespace Catalog.Api.Features.Edges.Sizes;

public sealed record GetEdgeSizeQuery(int Id) : IQuery<EdgeSizeDto>;

public sealed class GetEdgeSizeQueryHandler(CatalogDbContext dbContext)
    : IQueryHandler<GetEdgeSizeQuery, EdgeSizeDto>
{
    public async Task<EdgeSizeDto> Handle(GetEdgeSizeQuery query, CancellationToken ct) =>
        await dbContext.EdgeSizes
            .AsNoTracking()
            .Where(x => x.Id == query.Id)
            .Select(x => new EdgeSizeDto(
                x.Id,
                x.Name,
                x.Thickness,
                x.Width,
                x.OrderByCol,
                x.Edges.Count(),
                x.Edges.Count(edge => edge.IsStockItem),
                x.Edges.Count(edge => !edge.IsStockItem)))
            .SingleOrDefaultAsync(ct)
        ?? throw new BadHttpRequestException("Запись размера кромок не найдена.");
}