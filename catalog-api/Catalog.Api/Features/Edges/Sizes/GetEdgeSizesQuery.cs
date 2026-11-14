using Catalog.Api.Features.Edges.Sizes.Dto;
using Catalog.Database;
using Core.CQRS;

namespace Catalog.Api.Features.Edges.Sizes;

public sealed record GetEdgeSizesQuery : IQuery<GetEdgeSizesQueryResult>;

[Description("Список размеров кромок")]
public sealed record GetEdgeSizesQueryResult(
    [property: Description("Размеры кромок")] EdgeSizeDto[] Items);

public sealed class GetEdgeSizesQueryHandler(CatalogDbContext dbContext)
    : IQueryHandler<GetEdgeSizesQuery, GetEdgeSizesQueryResult>
{
    public async Task<GetEdgeSizesQueryResult> Handle(GetEdgeSizesQuery query, CancellationToken ct)
    {
        EdgeSizeDto[] items = await dbContext.EdgeSizes
            .AsNoTracking()
            .OrderBy(x => x.OrderByCol)
            .Select(x => new EdgeSizeDto(
                x.Id,
                x.Name,
                x.Thickness,
                x.Width,
                x.OrderByCol,
                x.Edges.Count(),
                x.Edges.Count(edge => edge.IsStockItem),
                x.Edges.Count(edge => !edge.IsStockItem)))
            .ToArrayAsync(ct);
        return new GetEdgeSizesQueryResult(items);
    }
}