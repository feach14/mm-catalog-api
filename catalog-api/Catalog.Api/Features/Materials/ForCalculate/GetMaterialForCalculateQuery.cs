using Catalog.Database;
using Core.CQRS;

namespace Catalog.Api.Features.Materials.ForCalculate;

public sealed record GetMaterialForCalculateQuery(int Id) : IQuery<GetMaterialForCalculateQueryResult>;

public sealed record GetMaterialForCalculateQueryResult(
    [property: Description("Id")] int Id,
    [property: Description("Площадь плиты, м²")] decimal KvM,
    [property: Description("Периметр плиты, м")] decimal PerimetrM);

public sealed class GetMaterialForCalculateQueryHandler(CatalogDbContext dbContext)
    : IQueryHandler<GetMaterialForCalculateQuery, GetMaterialForCalculateQueryResult>
{
    public async Task<GetMaterialForCalculateQueryResult> Handle(GetMaterialForCalculateQuery query, CancellationToken ct)
        => await dbContext.Materials.AsNoTracking()
               .Where(x => x.Id == query.Id)
               .Select(x => new GetMaterialForCalculateQueryResult(x.Id, x.KvM, x.PerimetrM))
               .SingleOrDefaultAsync(ct)
           ?? throw new BadHttpRequestException($"Материал с id={query.Id} не найден");
}