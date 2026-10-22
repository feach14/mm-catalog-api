using Catalog.Api.Features.Relations.ForAdmin.Dto;
using Catalog.Database;
using Core.CQRS;

namespace Catalog.Api.Features.Relations.ForAdmin;

public sealed record GetEdgeMaterialRelationsQuery(
    [property: Description("Id кромки")] int EdgeId) : IQuery<GetMaterialEdgeRelationsQueryResult>;

public sealed class GetEdgeMaterialRelationsQueryHandler(CatalogDbContext dbContext)
    : IQueryHandler<GetEdgeMaterialRelationsQuery, GetMaterialEdgeRelationsQueryResult>
{
    public async Task<GetMaterialEdgeRelationsQueryResult> Handle(GetEdgeMaterialRelationsQuery query, CancellationToken ct)
    {
        if (!await dbContext.Edges.AsNoTracking().AnyAsync(edge => edge.Id == query.EdgeId, ct))
            throw new BadHttpRequestException($"Кромка с id={query.EdgeId} не найдена.");

        var items = await dbContext.MaterialEdgeLinks.AsNoTracking()
            .Where(link => link.EdgeId == query.EdgeId)
            .OrderBy(link => link.MaterialId)
            .ThenBy(link => link.Priority)
            .ThenBy(link => link.EdgeId)
            .Select(link => new MaterialEdgeRelationDto(
                link.MaterialId, link.EdgeId, link.Priority,
                link.Markings.OrderBy(marking => marking.MarkingId).Select(marking => marking.MarkingId).ToArray()))
            .ToArrayAsync(ct);

        return new GetMaterialEdgeRelationsQueryResult(items);
    }
}