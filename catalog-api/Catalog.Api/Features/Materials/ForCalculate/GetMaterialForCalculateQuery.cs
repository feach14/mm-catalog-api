using Catalog.Api.Features.Materials.Helpers;
using Catalog.Database;
using Core.CQRS;

namespace Catalog.Api.Features.Materials.ForCalculate;

public sealed record GetMaterialForCalculateQuery(int Id) : IQuery<GetMaterialForCalculateQueryResult>;

public sealed record GetMaterialForCalculateQueryResult(
    [property: Description("Id")] int Id,
    [property: Description("Ширина плиты, мм")] int Width,
    [property: Description("Высота плиты, мм")] int Height,
    [property: Description("Стоимость листа, руб.: цена Стройдвора, если больше нуля, иначе ручная")] decimal Price,
    [property: Description("Название")] string Name,
    [property: Description("Артикул")] string Article,
    [property: Description("Комментарии для клиента")] string[] ClientComments,
    [property: Description("Материал есть в наличии")] bool InStock,
    [property: Description("Используется цена поставщика")] bool IsSupplierPrice);

public sealed class GetMaterialForCalculateQueryHandler(CatalogDbContext dbContext)
    : IQueryHandler<GetMaterialForCalculateQuery, GetMaterialForCalculateQueryResult>
{
    public async Task<GetMaterialForCalculateQueryResult> Handle(GetMaterialForCalculateQuery query, CancellationToken ct)
    {
        var material = await dbContext.Materials.AsNoTracking()
            .Where(x => x.Id == query.Id)
            .Select(x => new
            {
                x.Id,
                InStock = x.Count > 0 && (!x.StroydvorProductId.HasValue || x.StroydvorInStock),
                IsSupplierPrice = x.StroydvorPrice > 0,
                x.Name,
                x.Article,
                Price = x.StroydvorPrice > 0 ? x.StroydvorPrice : x.Price,
                x.MaterialSheetSize.Width,
                x.MaterialSheetSize.Height,
                MaterialTypeName = x.MaterialType.Name,
                Thickness = x.MaterialThickness.Value,
                ManufacturerName = x.MaterialManufacturer.Name
            })
            .Concat(dbContext.ArchivedMaterials.AsNoTracking()
                .Where(x => x.Id == query.Id)
                .Select(x => new
                {
                    x.Id,
                    InStock = x.Count > 0 && (!x.StroydvorProductId.HasValue || x.StroydvorInStock),
                    IsSupplierPrice = x.StroydvorPrice > 0,
                    x.Name,
                    x.Article,
                    Price = x.StroydvorPrice > 0 ? x.StroydvorPrice : x.Price,
                    x.MaterialSheetSize.Width,
                    x.MaterialSheetSize.Height,
                    MaterialTypeName = x.MaterialType.Name,
                    Thickness = x.MaterialThickness.Value,
                    ManufacturerName = x.MaterialManufacturer.Name
                }))
            .SingleOrDefaultAsync(ct)
            ?? throw new BadHttpRequestException($"Материал с id={query.Id} не найден");

        var clientComments = await dbContext.MaterialComments.AsNoTracking()
            .Where(comment => comment.IsForClient
                && (comment.MaterialId == material.Id || comment.ArchivedMaterialId == material.Id))
            .OrderBy(comment => comment.Id)
            .Select(comment => comment.Text)
            .ToArrayAsync(ct);

        return new GetMaterialForCalculateQueryResult(material.Id, material.Width, material.Height, material.Price,
            MaterialNameHelper.GetFullName(material.Name, material.MaterialTypeName, material.Thickness,
                material.Height, material.Width, material.ManufacturerName), material.Article, clientComments, material.InStock, material.IsSupplierPrice);
    }
}