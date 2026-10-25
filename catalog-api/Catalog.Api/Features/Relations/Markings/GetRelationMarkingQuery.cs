using Catalog.Api.Features.Relations.Markings.Dto;
using Catalog.Database;
using Core.CQRS;

namespace Catalog.Api.Features.Relations.Markings;

public sealed record GetRelationMarkingQuery(int Id) : IQuery<RelationMarkingDto>;

public sealed class GetRelationMarkingQueryHandler(CatalogDbContext dbContext)
    : IQueryHandler<GetRelationMarkingQuery, RelationMarkingDto>
{
    public async Task<RelationMarkingDto> Handle(GetRelationMarkingQuery query, CancellationToken ct) =>
        await dbContext.RelationMarkings
            .AsNoTracking()
            .Where(x => x.Id == query.Id)
            .Select(x => new RelationMarkingDto(
                x.Id,
                x.Name,
                x.Links.Count(),
                x.OrderByCol))
            .SingleOrDefaultAsync(ct)
        ?? throw new BadHttpRequestException("Маркировка не найдена.");
}