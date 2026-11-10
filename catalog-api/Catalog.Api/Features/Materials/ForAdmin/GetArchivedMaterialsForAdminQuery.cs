// ReSharper disable NotAccessedPositionalProperty.Global

using Catalog.Api.Features.Materials.Dto;
using Catalog.Database;
using Core.CQRS;

namespace Catalog.Api.Features.Materials.ForAdmin;

public sealed record GetArchivedMaterialsForAdminQuery : IQuery<GetAllMaterialsForAdminQueryResult>;

public class GetArchivedMaterialsForAdminQueryHandler(CatalogDbContext dbContext) : IQueryHandler<GetArchivedMaterialsForAdminQuery, GetAllMaterialsForAdminQueryResult>
{
    public async Task<GetAllMaterialsForAdminQueryResult> Handle(GetArchivedMaterialsForAdminQuery query, CancellationToken ct)
    {
        var materials = await dbContext.ArchivedMaterials.AsNoTracking()
            .OrderBy(x => x.OrderByCol).ThenBy(x => x.Id)
            .Select(x => new GetMaterialsQueryForAdminItemResult(
                x.Id,
                x.Name,
                x.Article,
                new PropertyDto(x.MaterialSheetSize.Id, x.MaterialSheetSize.Name),
                new PropertyDto(x.MaterialManufacturer.Id, x.MaterialManufacturer.Name),
                new PropertyDto(x.MaterialType.Id, x.MaterialType.Name),
                new PropertyDto(x.MaterialThickness.Id, x.MaterialThickness.Name),
                new PropertyDto(x.CategoryId, x.Category.Name),
                x.ExternalLink,
                x.Count,
                x.OrderByCol,
                x.Price,
                x.HideInLk,
                x.HideOnSite,
                x.HidePriceOnSite,
                x.StroydvorProductId,
                x.StroydvorArticle,
                x.StroydvorPrice,
                x.StroydvorLastSyncAt,
                x.StroydvorInStock))
            .ToArrayAsync(ct);

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

        return new GetAllMaterialsForAdminQueryResult(materials,
            new MaterialsForAdminFiltersDto(categories, manufacturers, materialTypes, thicknesses, sheetSizes));
    }
}