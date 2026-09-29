using Catalog.Api.Features.MaterialThicknesses.Dto;
using Catalog.Database;
using Core.CQRS;

namespace Catalog.Api.Features.MaterialThicknesses;

public sealed record GetMaterialThicknessesQuery : IQuery<GetMaterialThicknessesQueryResult>;
public sealed record GetMaterialThicknessesQueryResult(
    [property: Description("Список толщин материалов")] MaterialThicknessDto[] Items);

public sealed class GetMaterialThicknessesQueryHandler(CatalogDbContext dbContext)
    : IQueryHandler<GetMaterialThicknessesQuery, GetMaterialThicknessesQueryResult>
{
    public async Task<GetMaterialThicknessesQueryResult> Handle(GetMaterialThicknessesQuery query, CancellationToken ct)
    {
        var items = await dbContext.MaterialThicknesses
            .AsNoTracking()
            .OrderBy(x => x.Value)
            .Select(x => new MaterialThicknessDto(x.Id, x.Name, x.Value))
            .ToArrayAsync(ct);
        return new GetMaterialThicknessesQueryResult(items);
    }
}