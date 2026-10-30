using Catalog.Api.Features.Materials.Helpers;
using Catalog.Database;
using Core.CQRS;

namespace Catalog.Api.Features.Materials.ForVekLk;

public sealed record GetCuttingMaterialQuery(int Id) : IQuery<GetCuttingMaterialQueryResult>;

public sealed record GetCuttingMaterialQueryResult(
    [property: Description("Id")] int Id,
    [property: Description("Название")] string Name);

public sealed class GetCuttingMaterialQueryHandler(CatalogDbContext dbContext)
    : IQueryHandler<GetCuttingMaterialQuery, GetCuttingMaterialQueryResult>
{
    public async Task<GetCuttingMaterialQueryResult> Handle(GetCuttingMaterialQuery query, CancellationToken ct)
    {
        var material = await dbContext.Materials.AsNoTracking()
            .Include(x => x.MaterialType)
            .Include(x => x.MaterialThickness)
            .Include(x => x.MaterialSheetSize)
            .Include(x => x.MaterialManufacturer)
            .Where(x => x.Id == query.Id && !x.HideInLk && !x.Category.HideInLk)
            .SingleOrDefaultAsync(ct)
            ?? throw new BadHttpRequestException("Материал не найден.");

        return new GetCuttingMaterialQueryResult(material.Id, MaterialNameHelper.GetFullName(material));
    }
}