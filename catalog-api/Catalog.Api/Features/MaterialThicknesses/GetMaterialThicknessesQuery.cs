using Catalog.Database;
using Core.CQRS;

namespace Catalog.Api.Features.MaterialThicknesses;

public sealed record GetMaterialThicknessesQuery : IQuery<GetMaterialThicknessesQueryResult>;

public sealed record GetMaterialThicknessesQueryResult(
    [property: Description("Список толщин материалов")] GetMaterialThicknessesQueryItem[] Items);

public sealed record GetMaterialThicknessesQueryItem(
    [property: Description("Id толщины материала")] int Id,
    [property: Description("Название толщины материала")] string Name,
    [property: Description("Толщина в миллиметрах")] double Value,
    [property: Description("Количество материалов в наличии")] int MaterialsAnyCount,
    [property: Description("Количество материалов не в наличии")] int MaterialsNotAnyCount);

public sealed class GetMaterialThicknessesQueryHandler(CatalogDbContext dbContext)
    : IQueryHandler<GetMaterialThicknessesQuery, GetMaterialThicknessesQueryResult>
{
    public async Task<GetMaterialThicknessesQueryResult> Handle(GetMaterialThicknessesQuery query, CancellationToken ct)
    {
        var items = await dbContext.MaterialThicknesses
            .AsNoTracking()
            .OrderBy(x => x.Value)
            .Select(x => new GetMaterialThicknessesQueryItem(
                x.Id,
                x.Name,
                x.Value,
                x.Materials.Count(material => material.Count > 0),
                x.Materials.Count(material => material.Count < 1)))
            .ToArrayAsync(ct);
        return new GetMaterialThicknessesQueryResult(items);
    }
}