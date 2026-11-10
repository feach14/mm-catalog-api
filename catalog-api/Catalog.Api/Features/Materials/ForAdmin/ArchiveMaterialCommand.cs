using Catalog.Api.Infrastructure.Ordering;
using Catalog.Api.Services.History;
using Catalog.Database;
using Catalog.Database.Entities;
using Catalog.Database.Enums;
using Core.CQRS;

namespace Catalog.Api.Features.Materials.ForAdmin;

public sealed record ArchiveMaterialCommand(int Id) : ICommand<ArchiveMaterialCommandResult>;

public sealed record ArchiveMaterialCommandResult(
    [property: Description("Успех операции")] bool Success);

public sealed class ArchiveMaterialCommandHandler(CatalogDbContext dbContext, ICatalogHistoryWriter historyWriter)
    : ICommandHandler<ArchiveMaterialCommand, ArchiveMaterialCommandResult>
{
    public async Task<ArchiveMaterialCommandResult> Handle(ArchiveMaterialCommand command, CancellationToken ct)
    {
        return await CatalogOrderTransaction.Run(dbContext, async () =>
        {
            var source = await dbContext.Materials
                .SingleOrDefaultAsync(material => material.Id == command.Id, ct);
            if (source is null)
            {
                if (await dbContext.ArchivedMaterials.AsNoTracking().AnyAsync(material => material.Id == command.Id, ct))
                    return new ArchiveMaterialCommandResult(true);
                throw new BadHttpRequestException("Материал не найден.");
            }
            if (await dbContext.ArchivedMaterials.AsNoTracking().AnyAsync(material => material.Id == command.Id, ct))
                throw new BadHttpRequestException("Материал уже существует в целевом списке.");
            var position = await dbContext.ArchivedMaterials.MaxAsync(material => (int?)material.OrderByCol, ct) ?? 0;
            if (position == int.MaxValue)
                throw new BadHttpRequestException("Достигнут предел порядка материалов в целевом списке.");
            var destination = new ArchivedMaterial
            {
                Id = source.Id,
                OriginalMaterialId = source.Id,
                OriginalOrderByCol = source.OrderByCol,
                CategoryId = source.CategoryId,
                MaterialSheetSizeId = source.MaterialSheetSizeId,
                MaterialTypeId = source.MaterialTypeId,
                MaterialManufacturerId = source.MaterialManufacturerId,
                Name = source.Name,
                Article = source.Article,
                MaterialThicknessId = source.MaterialThicknessId,
                Count = source.Count,
                ExternalLink = source.ExternalLink,
                CommentOnMaterialIsRequired = source.CommentOnMaterialIsRequired,
                AllowSecondItemInOrder = source.AllowSecondItemInOrder,
                Price = source.Price,
                StroydvorProductId = source.StroydvorProductId,
                StroydvorArticle = source.StroydvorArticle,
                StroydvorPrice = source.StroydvorPrice,
                StroydvorLastSyncAt = source.StroydvorLastSyncAt,
                StroydvorInStock = source.StroydvorInStock,
                HideOnSite = source.HideOnSite,
                HideInLk = source.HideInLk,
                HidePriceOnSite = source.HidePriceOnSite,
                CountTypeEnum = source.CountTypeEnum,
                OrderByCol = position + 1
            };
            dbContext.ArchivedMaterials.Add(destination);
            await dbContext.SaveChangesAsync(ct);

            // Existing child rows keep their identity; only their material owner changes.
            // The source is tracked without children, so deleting it cannot orphan the moved rows in the change tracker.
            var imageCount = await dbContext.MaterialImages.Where(image => image.MaterialId == source.Id)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(image => image.ArchivedMaterialId, destination.Id)
                    .SetProperty(image => image.MaterialId, (int?)null), ct);
            var linkCount = await dbContext.MaterialEdgeLinks.Where(link => link.MaterialId == source.Id)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(link => link.ArchivedMaterialId, destination.Id)
                    .SetProperty(link => link.MaterialId, (int?)null), ct);
            dbContext.Materials.Remove(source);
            historyWriter.Add(CatalogHistoryActionTypeEnum.Update, CatalogHistoryEntityTypeEnum.Material, destination.Id,
                $"Материал #{destination.Id} «{destination.Name}» перенесён в архив: изображений {imageCount}, связей с кромками {linkCount}.");
            await dbContext.SaveChangesAsync(ct);
            return new ArchiveMaterialCommandResult(true);
        }, ct);
    }
}