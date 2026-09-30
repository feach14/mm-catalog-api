using Catalog.Database;
using Core.CQRS;

namespace Catalog.Api.Features.MaterialThicknesses.ForAdmin;

public sealed record GetMaterialThicknessQuery(int Id) : IQuery<GetMaterialThicknessQueryResult>;

public sealed record GetMaterialThicknessQueryResult(
    [property: Description("Id толщины материала")] int Id,
    [property: Description("Название толщины материала")] string Name,
    [property: Description("Толщина в миллиметрах")] decimal Value);

public sealed class GetMaterialThicknessQueryHandler(CatalogDbContext dbContext)
    : IQueryHandler<GetMaterialThicknessQuery, GetMaterialThicknessQueryResult>
{
    public async Task<GetMaterialThicknessQueryResult> Handle(GetMaterialThicknessQuery query, CancellationToken ct)
    {
        var item = await dbContext.MaterialThicknesses
            .AsNoTracking()
            .Where(x => x.Id == query.Id)
            .Select(x => new GetMaterialThicknessQueryResult(
                x.Id,
                x.Name,
                x.Value))
            .SingleOrDefaultAsync(ct);
        return item ?? throw new BadHttpRequestException($"Толщина материала с id={query.Id} не найдена.");
    }
}