using Catalog.Api.Features.Materials.Dto;
using Catalog.Api.Features.Materials.ForAdmin.Dto;
using Catalog.Database;
using Catalog.Database.Enums;
using Core.BaseEnums;
using Core.CQRS;

namespace Catalog.Api.Features.Materials.ForAdmin;

public sealed record GetArchivedMaterialForAdminQuery(int Id) : IQuery<GetMaterialForAdminQueryResult>;

public class GetArchivedMaterialForAdminQueryHandler(CatalogDbContext dbContext) : IQueryHandler<GetArchivedMaterialForAdminQuery, GetMaterialForAdminQueryResult>
{
    public async Task<GetMaterialForAdminQueryResult> Handle(GetArchivedMaterialForAdminQuery query, CancellationToken ct) =>
        await dbContext.ArchivedMaterials.AsNoTracking()
            .Where(x => x.Id == query.Id)
            .Select(x => new GetMaterialForAdminQueryResult(
                x.Id,
                x.Name,
                x.Article,
                new PropertyDto(x.CategoryId, x.Category.Name),
                new PropertyDto(x.MaterialSheetSize.Id, x.MaterialSheetSize.Name),
                new PropertyDto(x.MaterialManufacturer.Id, x.MaterialManufacturer.Name),
                new PropertyDto(x.MaterialType.Id, x.MaterialType.Name),
                new PropertyDto(x.MaterialThickness.Id, x.MaterialThickness.Name),
                new MaterialImagesDto
                {
                    Original = x.Images
                        .Where(image => image.ImageType == MaterialImageTypeEnum.Original)
                        .Select(image => (Guid?)image.Guid)
                        .SingleOrDefault(),
                    OriginalMetadata = x.Images
                        .Where(image => image.ImageType == MaterialImageTypeEnum.Original)
                        .Select(image => new MaterialImageMetadataDto
                        {
                            ContentType = image.ContentType,
                            Size = image.Data.Length
                        })
                        .SingleOrDefault(),
                    Thumbnail240 = x.Images
                        .Where(image => image.ImageType == MaterialImageTypeEnum.Thumbnail240)
                        .Select(image => (Guid?)image.Guid)
                        .SingleOrDefault(),
                    Thumbnail240Metadata = x.Images
                        .Where(image => image.ImageType == MaterialImageTypeEnum.Thumbnail240)
                        .Select(image => new MaterialImageMetadataDto
                        {
                            ContentType = image.ContentType,
                            Size = image.Data.Length
                        })
                        .SingleOrDefault(),
                    Thumbnail480 = x.Images
                        .Where(image => image.ImageType == MaterialImageTypeEnum.Thumbnail480)
                        .Select(image => (Guid?)image.Guid)
                        .SingleOrDefault(),
                    Thumbnail480Metadata = x.Images
                        .Where(image => image.ImageType == MaterialImageTypeEnum.Thumbnail480)
                        .Select(image => new MaterialImageMetadataDto
                        {
                            ContentType = image.ContentType,
                            Size = image.Data.Length
                        })
                        .SingleOrDefault()
                },
                x.Count,
                x.OrderByCol,
                x.CommentOnMaterialIsRequired,
                x.AllowSecondItemInOrder,
                x.ExternalLink,
                x.Price,
                x.HideInLk,
                x.HideOnSite,
                x.HidePriceOnSite,
                x.CountTypeEnum,
                x.StroydvorProductId,
                x.StroydvorArticle,
                x.StroydvorPrice,
                x.StroydvorLastSyncAt,
                x.StroydvorInStock,
                true,
                x.OriginalMaterialId))
            .SingleOrDefaultAsync(ct)
        ?? throw new BadHttpRequestException($"Материал с id={query.Id} не найден");
}