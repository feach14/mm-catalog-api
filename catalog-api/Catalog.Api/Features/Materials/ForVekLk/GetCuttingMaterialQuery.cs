using Catalog.Api.Features.Materials.Helpers;
using Catalog.Database;
using Core.CQRS;

namespace Catalog.Api.Features.Materials.ForVekLk;

public sealed record GetCuttingMaterialQuery(int Id) : IQuery<GetCuttingMaterialQueryResult>;

public sealed record GetCuttingMaterialQueryResult(
    [property: Description("Id")] int Id,
    [property: Description("Название")] string Name,
    [property: Description("Разрешена вторая позиция в заявке")] bool AllowSecondItemInOrder,
    [property: Description("Ссылка на внешний источник")] string? ExternalLink);

public sealed class GetCuttingMaterialQueryHandler(CatalogDbContext dbContext)
    : IQueryHandler<GetCuttingMaterialQuery, GetCuttingMaterialQueryResult>
{
    public async Task<GetCuttingMaterialQueryResult> Handle(GetCuttingMaterialQuery query, CancellationToken ct)
    {
        var material = await dbContext.Materials.AsNoTracking()
            .Where(x => x.Id == query.Id)
            .Select(x => new
            {
                x.Id,
                x.Name,
                MaterialTypeName = x.MaterialType.Name,
                Thickness = x.MaterialThickness.Value,
                x.MaterialSheetSize.Height,
                x.MaterialSheetSize.Width,
                ManufacturerName = x.MaterialManufacturer.Name,
                x.AllowSecondItemInOrder,
                ExternalLink = string.IsNullOrWhiteSpace(x.ExternalLink) ? x.Category.ExternalLink : x.ExternalLink
            })
            .SingleOrDefaultAsync(ct)
            ?? throw new BadHttpRequestException("Материал не найден.");

        return new GetCuttingMaterialQueryResult(material.Id,
            MaterialNameHelper.GetFullName(material.Name, material.MaterialTypeName, material.Thickness,
                material.Height, material.Width, material.ManufacturerName),
            material.AllowSecondItemInOrder, material.ExternalLink);
    }
}