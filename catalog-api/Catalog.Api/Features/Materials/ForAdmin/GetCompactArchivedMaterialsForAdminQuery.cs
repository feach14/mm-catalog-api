// ReSharper disable NotAccessedPositionalProperty.Global

using Catalog.Database;
using Catalog.Database.Enums;
using Core.CQRS;

namespace Catalog.Api.Features.Materials.ForAdmin;

public sealed record GetCompactArchivedMaterialsForAdminQuery : IQuery<GetCompactMaterialsForAdminQueryResult>;

public sealed class GetCompactArchivedMaterialsForAdminQueryHandler(
    IQueryHandler<GetArchivedMaterialsForAdminQuery, GetAllMaterialsForAdminQueryResult> materialsHandler,
    CatalogDbContext dbContext)
    : IQueryHandler<GetCompactArchivedMaterialsForAdminQuery, GetCompactMaterialsForAdminQueryResult>
{
    public async Task<GetCompactMaterialsForAdminQueryResult> Handle(GetCompactArchivedMaterialsForAdminQuery query, CancellationToken ct)
    {
        var materials = await materialsHandler.Handle(new GetArchivedMaterialsForAdminQuery(), ct);
        var thumbnails = await dbContext.MaterialImages.AsNoTracking()
            .Where(image => image.ArchivedMaterialId.HasValue
                && (image.ImageType == MaterialImageTypeEnum.Thumbnail240
                    || image.ImageType == MaterialImageTypeEnum.Thumbnail480))
            .OrderBy(image => image.ImageType == MaterialImageTypeEnum.Thumbnail240 ? 0 : 1)
            .Select(image => new { MaterialId = image.ArchivedMaterialId.GetValueOrDefault(), Guid = (Guid?)image.Guid })
            .ToArrayAsync(ct);
        var previewByMaterial = thumbnails.GroupBy(image => image.MaterialId)
            .ToDictionary(group => group.Key, group => group.First().Guid);
        var items = materials.Items.Select(x => new GetCompactMaterialsForAdminQueryItem(
            x.Id,
            x.Name,
            x.Article,
            x.SheetSize.Id,
            x.Manufacturer.Id,
            x.MaterialType.Id,
            x.Thickness.Id,
            x.Category.Id,
            x.Count,
            x.OrderByCol,
            x.Price,
            x.HideInLk,
            x.HideOnSite,
            x.HidePriceOnSite,
            previewByMaterial.GetValueOrDefault(x.Id),
            x.ExternalLink,
            x.StroydvorProductId,
            x.StroydvorArticle,
            x.StroydvorPrice,
            x.StroydvorLastSyncAt,
            x.StroydvorInStock,
            x.StroydvorUrl)).ToArray();

        return new GetCompactMaterialsForAdminQueryResult(items, materials.Filters);
    }
}