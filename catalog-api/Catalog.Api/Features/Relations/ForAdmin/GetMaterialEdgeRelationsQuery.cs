using Catalog.Api.Features.Relations.ForAdmin.Dto;
using Catalog.Database;
using Core.CQRS;

namespace Catalog.Api.Features.Relations.ForAdmin;

public sealed record GetMaterialEdgeRelationsQuery(
    [property: Description("Id материала")] int MaterialId) : IQuery<GetMaterialEdgeRelationsQueryResult>;

public sealed record GetMaterialEdgeRelationsQueryResult(
    [property: Description("Связи материалов и кромок")] MaterialEdgeRelationDto[] Items);

public sealed class GetMaterialEdgeRelationsQueryHandler(CatalogDbContext dbContext)
    : IQueryHandler<GetMaterialEdgeRelationsQuery, GetMaterialEdgeRelationsQueryResult>
{
    public async Task<GetMaterialEdgeRelationsQueryResult> Handle(GetMaterialEdgeRelationsQuery query, CancellationToken ct)
    {
        if (!await dbContext.Materials.AsNoTracking().AnyAsync(material => material.Id == query.MaterialId, ct))
            throw new BadHttpRequestException($"Материал с id={query.MaterialId} не найден.");

        var items = await dbContext.MaterialEdgeLinks.AsNoTracking()
            .Where(link => link.MaterialId == query.MaterialId)
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