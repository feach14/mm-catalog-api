using Catalog.Api.Features.Edges.Helpers;
using Catalog.Api.Features.Materials.Dto;
using Catalog.Api.Features.Materials.Helpers;
using Catalog.Database;
using Catalog.Database.Enums;
using Core.CQRS;

namespace Catalog.Api.Features.Materials.ForVekLk;

public sealed record GetCuttingMaterialQuery(int Id) : IQuery<GetCuttingMaterialQueryResult>;

public sealed record GetCuttingMaterialQueryResult(
    [property: Description("Id")] int Id,
    [property: Description("Название")] string Name,
    [property: Description("Разрешена вторая позиция в заявке")] bool AllowSecondItemInOrder,
    [property: Description("Ссылка на внешний источник")] string? ExternalLink,
    [property: Description("Прикреплённые кромки")] CuttingMaterialEdgeDto[] Edges);

public sealed record CuttingMaterialEdgeDto(
    [property: Description("Id")] int Id,
    [property: Description("Полное название")] string Name,
    [property: Description("Артикул")] string Article,
    [property: Description("Стоимость: за метр для Meter, за бухту для Roll, руб.")] decimal Price,
    [property: Description("Толщина, мм")] decimal Thickness,
    [property: Description("Ширина, мм")] decimal Width,
    [property: Description("Складская позиция")] bool IsStockItem,
    [property: Description("Длина бухты или кратность отреза в метрах")] decimal QuantityMultiple,
    [property: Description("Тип: Meter — погонный метр, Roll — бухта"), JsonConverter(typeof(StrictStringEnumConverter))] EdgeSaleUnitTypeEnum SaleUnitType,
    [property: Description("Приоритет связи")] int Priority,
    [property: Description("Маркировки связи с материалом")] PropertyDto[] Markings);

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
            .Concat(dbContext.ArchivedMaterials.AsNoTracking()
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
                }))
            .SingleOrDefaultAsync(ct)
            ?? throw new BadHttpRequestException("Материал не найден.");

        var links = await dbContext.MaterialEdgeLinks.AsNoTracking()
            .Where(link => link.OwnerMaterialId == material.Id)
            .OrderBy(link => link.Priority)
            .Select(link => new
            {
                link.Edge.Id,
                link.Edge.Name,
                link.Edge.Article,
                Price = link.Edge.StroydvorPrice > 0 ? link.Edge.StroydvorPrice : link.Edge.Price,
                SizeName = link.Edge.Size.Name,
                link.Edge.Size.Thickness,
                link.Edge.Size.Width,
                link.Edge.IsStockItem,
                QuantityMultiple = link.Edge.SaleUnit.Length,
                link.Edge.SaleUnit.SaleUnitType,
                link.Priority,
                Markings = link.Markings.OrderBy(marking => marking.Marking.OrderByCol)
                    .Select(marking => new PropertyDto(marking.MarkingId, marking.Marking.Name)).ToArray()
            })
            .ToArrayAsync(ct);
        var edges = links.Select(link => new CuttingMaterialEdgeDto(link.Id,
            EdgeNameHelper.GetFullName(link.Name, link.SizeName, link.IsStockItem), link.Article, link.Price,
            link.Thickness, link.Width, link.IsStockItem,
            link.QuantityMultiple, link.SaleUnitType, link.Priority, link.Markings)).ToArray();

        return new GetCuttingMaterialQueryResult(material.Id,
            MaterialNameHelper.GetFullName(material.Name, material.MaterialTypeName, material.Thickness,
                material.Height, material.Width, material.ManufacturerName),
            material.AllowSecondItemInOrder, material.ExternalLink, edges);
    }
}