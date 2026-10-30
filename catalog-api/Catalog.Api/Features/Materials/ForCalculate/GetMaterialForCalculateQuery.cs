using Catalog.Api.Features.Materials.Helpers;
using Catalog.Database;
using Core.CQRS;

namespace Catalog.Api.Features.Materials.ForCalculate;

public sealed record GetMaterialForCalculateQuery(int Id) : IQuery<GetMaterialForCalculateQueryResult>;

public sealed record GetMaterialForCalculateQueryResult(
    [property: Description("Id")] int Id,
    [property: Description("Ширина плиты, мм")] int Width,
    [property: Description("Высота плиты, мм")] int Height,
    [property: Description("Стоимость листа, руб.")] decimal Price,
    [property: Description("Название")] string Name,
    [property: Description("Артикул")] string Article);

public sealed class GetMaterialForCalculateQueryHandler(CatalogDbContext dbContext)
    : IQueryHandler<GetMaterialForCalculateQuery, GetMaterialForCalculateQueryResult>
{
    public async Task<GetMaterialForCalculateQueryResult> Handle(GetMaterialForCalculateQuery query, CancellationToken ct)
    {
        var material = await dbContext.Materials.AsNoTracking()
            .Include(x => x.MaterialType)
            .Include(x => x.MaterialThickness)
            .Include(x => x.MaterialSheetSize)
            .Include(x => x.MaterialManufacturer)
            .SingleOrDefaultAsync(x => x.Id == query.Id, ct)
            ?? throw new BadHttpRequestException($"Материал с id={query.Id} не найден");

        return new GetMaterialForCalculateQueryResult(material.Id, material.MaterialSheetSize.Width,
            material.MaterialSheetSize.Height, material.Price, MaterialNameHelper.GetFullName(material), material.Article);
    }
}