using Catalog.Api.Features.Relations.Markings.Dto;
using Catalog.Database;
using Core.CQRS;

namespace Catalog.Api.Features.Relations.Markings;

public sealed record GetRelationMarkingsQuery : IQuery<GetRelationMarkingsQueryResult>;

[Description("Список маркировок")]
public sealed record GetRelationMarkingsQueryResult(
    [property: Description("Маркировки")] RelationMarkingDto[] Items);

public sealed class GetRelationMarkingsQueryHandler(CatalogDbContext dbContext)
    : IQueryHandler<GetRelationMarkingsQuery, GetRelationMarkingsQueryResult>
{
    public async Task<GetRelationMarkingsQueryResult> Handle(GetRelationMarkingsQuery query, CancellationToken ct)
    {
        RelationMarkingDto[] items = await dbContext.RelationMarkings
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .ThenBy(x => x.Id)
            .Select(x => new RelationMarkingDto(
                x.Id,
                x.Name,
                x.Links.Count()))
            .ToArrayAsync(ct);
        return new GetRelationMarkingsQueryResult(items);
    }
}