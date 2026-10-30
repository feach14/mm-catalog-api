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
            .Where(x => x.Id == query.Id && !x.HideInLk && !x.Category.HideInLk)
            .Select(x => new
            {
                x.Id,
                x.Name,
                MaterialTypeName = x.MaterialType.Name,
                Thickness = x.MaterialThickness.Value,
                x.MaterialSheetSize.Height,
                x.MaterialSheetSize.Width,
                ManufacturerName = x.MaterialManufacturer.Name
            })
            .SingleOrDefaultAsync(ct)
            ?? throw new BadHttpRequestException("Материал не найден.");

        return new GetCuttingMaterialQueryResult(material.Id,
            VekLkMaterialName.Format(material.MaterialTypeName, material.Name, material.Thickness, material.Height, material.Width, material.ManufacturerName));
    }
}