// ReSharper disable NotAccessedPositionalProperty.Global

using Catalog.Api.Features.Materials.Dto;
using Catalog.Api.Features.Materials.ForAdmin.Dto;
using Catalog.Api.Features.Materials.Helpers;
using Catalog.Database;
using Catalog.Database.Enums;
using Core.CQRS;

namespace Catalog.Api.Features.Materials.ForAdmin;

public sealed record GetCompactArchivedMaterialsForAdminQuery : IQuery<GetCompactMaterialsForAdminQueryResult>;

public sealed class GetCompactArchivedMaterialsForAdminQueryHandler(CatalogDbContext dbContext)
    : IQueryHandler<GetCompactArchivedMaterialsForAdminQuery, GetCompactMaterialsForAdminQueryResult>
{
    public async Task<GetCompactMaterialsForAdminQueryResult> Handle(GetCompactArchivedMaterialsForAdminQuery query, CancellationToken ct)
    {
        var items = await dbContext.ArchivedMaterials.AsNoTracking()
            .OrderBy(x => x.OrderByCol)
            .Select(x => new GetCompactMaterialsForAdminQueryItem(
                x.Id,
                x.Name,
                x.Article,
                x.MaterialSheetSizeId,
                x.MaterialManufacturerId,
                x.MaterialTypeId,
                x.MaterialThicknessId,
                x.CategoryId,
                x.Count,
                x.OrderByCol,
                x.Price,
                x.HideInLk,
                x.HideOnSite,
                x.HidePriceOnSite,
                x.Images
                    .Where(image => image.ImageType == MaterialImageTypeEnum.Thumbnail240
                        || image.ImageType == MaterialImageTypeEnum.Thumbnail480)
                    .OrderBy(image => image.ImageType == MaterialImageTypeEnum.Thumbnail240 ? 0 : 1)
                    .Select(image => (Guid?)image.Guid)
                    .FirstOrDefault(),
                x.ExternalLink,
                x.StroydvorEnabled,
                x.StroydvorProductId,
                x.StroydvorArticle,
                x.StroydvorPrice,
                x.StroydvorLastSyncAt,
                x.StroydvorInStock,
                x.StroydvorProductId.HasValue ? $"https://stroydvor21.ru/products/{x.StroydvorProductId.Value}" : null,
                x.Comments.OrderBy(comment => comment.Id)
                    .Select(comment => new MaterialCommentDto(comment.Id, comment.Text, comment.IsForClient)).ToArray(),
                null))
            .ToArrayAsync(ct);

        if (items.Length > 0)
        {
            var percentages = MaterialClientPricePercentages.From(await dbContext.PricePercentages.AsNoTracking()
                .Where(percentage => percentage.AppliesTo == PricePercentageTargetEnum.MATERIAL)
                .ToDictionaryAsync(percentage => percentage.Key, percentage => percentage.Value, ct));
            items = items.Select(material => material with
            {
                ClientPrices = percentages.GetPrices(material.P, material.Spp)
            }).ToArray();
        }

        var categories = await dbContext.MaterialCategories.AsNoTracking()
            .OrderBy(x => x.OrderByCol)
            .Select(x => new MaterialsForAdminCategoryFilterDto(x.Id, x.Name, x.HideOnSite, x.HideInLk))
            .ToArrayAsync(ct);

        var manufacturers = await dbContext.MaterialManufacturers.AsNoTracking()
            .OrderBy(x => x.OrderByCol)
            .Select(x => new PropertyDto(x.Id, x.Name))
            .ToArrayAsync(ct);

        var materialTypes = await dbContext.MaterialTypes.AsNoTracking()
            .OrderBy(x => x.OrderByCol)
            .Select(x => new PropertyDto(x.Id, x.Name))
            .ToArrayAsync(ct);

        var thicknesses = await dbContext.MaterialThicknesses.AsNoTracking()
            .OrderBy(x => x.Value)
            .Select(x => new MaterialsForAdminThicknessFilterDto(x.Id, x.Name, x.Value))
            .ToArrayAsync(ct);

        var sheetSizes = await dbContext.MaterialSheetSizes.AsNoTracking()
            .OrderBy(x => x.OrderByCol)
            .Select(x => new MaterialsForAdminSheetSizeFilterDto(
                x.Id,
                x.Name,
                x.Height,
                x.Width,
                x.ShowInFilters))
            .ToArrayAsync(ct);

        return new GetCompactMaterialsForAdminQueryResult(items,
            new MaterialsForAdminFiltersDto(categories, manufacturers, materialTypes, thicknesses, sheetSizes));
    }
}