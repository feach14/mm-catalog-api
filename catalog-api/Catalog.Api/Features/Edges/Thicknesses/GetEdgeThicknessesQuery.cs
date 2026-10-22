using Catalog.Api.Features.Edges.Thicknesses.Dto;
using Catalog.Database;
using Core.CQRS;

namespace Catalog.Api.Features.Edges.Thicknesses;

public sealed record GetEdgeThicknessesQuery : IQuery<GetEdgeThicknessesQueryResult>;

[Description("Список толщин кромок")]
public sealed record GetEdgeThicknessesQueryResult(
    [property: Description("Толщины кромок")] EdgeThicknessDto[] Items);

public sealed class GetEdgeThicknessesQueryHandler(CatalogDbContext dbContext)
    : IQueryHandler<GetEdgeThicknessesQuery, GetEdgeThicknessesQueryResult>
{
    public async Task<GetEdgeThicknessesQueryResult> Handle(GetEdgeThicknessesQuery query, CancellationToken ct)
    {
        EdgeThicknessDto[] items = await dbContext.EdgeThicknesses
            .AsNoTracking()
            .OrderBy(x => x.Value)
            .ThenBy(x => x.Id)
            .Select(x => new EdgeThicknessDto(
                x.Id,
                x.Name,
                x.Value,
                x.Edges.Count(),
                x.Edges.Count(edge => edge.IsStockItem),
                x.Edges.Count(edge => !edge.IsStockItem)))
            .ToArrayAsync(ct);
        return new GetEdgeThicknessesQueryResult(items);
    }
}