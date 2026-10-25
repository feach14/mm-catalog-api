using Catalog.Api.Features.Relations.ForAdmin.Dto;
using Catalog.Database;
using Core.CQRS;

namespace Catalog.Api.Features.Relations.ForAdmin;

public sealed record GetCompactMaterialEdgeRelationsQuery : IQuery<GetCompactMaterialEdgeRelationsQueryResult>;

public sealed record GetCompactMaterialEdgeRelationsQueryResult(
    [property: Description("Связи материалов и кромок")] MaterialEdgeRelationDto[] Items,
    [property: Description("Справочники для фильтров и редактора")] MaterialEdgeRelationFiltersDto Filters);

public sealed class GetCompactMaterialEdgeRelationsQueryHandler(CatalogDbContext dbContext)
    : IQueryHandler<GetCompactMaterialEdgeRelationsQuery, GetCompactMaterialEdgeRelationsQueryResult>
{
    public async Task<GetCompactMaterialEdgeRelationsQueryResult> Handle(GetCompactMaterialEdgeRelationsQuery query, CancellationToken ct)
    {
        var items = await dbContext.MaterialEdgeLinks.AsNoTracking()
            .OrderBy(link => link.MaterialId)
            .ThenBy(link => link.Priority)
            .ThenBy(link => link.EdgeId)
            .Select(link => new MaterialEdgeRelationDto(
                link.MaterialId, link.EdgeId, link.Priority,
                link.Markings.OrderBy(marking => marking.MarkingId).Select(marking => marking.MarkingId).ToArray()))
            .ToArrayAsync(ct);
        var markings = await dbContext.RelationMarkings.AsNoTracking()
            .OrderBy(marking => marking.OrderByCol)
            .ThenBy(marking => marking.Id)
            .Select(marking => new RelationMarkingOptionDto(marking.Id, marking.Name, marking.OrderByCol))
            .ToArrayAsync(ct);

        return new GetCompactMaterialEdgeRelationsQueryResult(items, new MaterialEdgeRelationFiltersDto(markings));
    }
}