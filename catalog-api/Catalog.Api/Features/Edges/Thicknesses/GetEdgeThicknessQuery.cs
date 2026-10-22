using Catalog.Api.Features.Edges.Thicknesses.Dto;
using Catalog.Database;
using Core.CQRS;

namespace Catalog.Api.Features.Edges.Thicknesses;

public sealed record GetEdgeThicknessQuery(int Id) : IQuery<EdgeThicknessDto>;

public sealed class GetEdgeThicknessQueryHandler(CatalogDbContext dbContext)
    : IQueryHandler<GetEdgeThicknessQuery, EdgeThicknessDto>
{
    public async Task<EdgeThicknessDto> Handle(GetEdgeThicknessQuery query, CancellationToken ct) =>
        await dbContext.EdgeThicknesses
            .AsNoTracking()
            .Where(x => x.Id == query.Id)
            .Select(x => new EdgeThicknessDto(
                x.Id,
                x.Name,
                x.Value,
                x.Edges.Count(),
                x.Edges.Count(edge => edge.IsStockItem),
                x.Edges.Count(edge => !edge.IsStockItem)))
            .SingleOrDefaultAsync(ct)
        ?? throw new BadHttpRequestException("Запись толщины кромок не найдена.");
}