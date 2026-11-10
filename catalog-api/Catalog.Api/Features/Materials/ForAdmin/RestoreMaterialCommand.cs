using Catalog.Api.Infrastructure.Ordering;
using Catalog.Api.Services.History;
using Catalog.Database;
using Catalog.Database.Entities;
using Catalog.Database.Enums;
using Core.CQRS;

namespace Catalog.Api.Features.Materials.ForAdmin;

public sealed record RestoreMaterialCommand(int Id) : ICommand<RestoreMaterialCommandResult>;

public sealed record RestoreMaterialCommandResult(
    [property: Description("Успех операции")] bool Success);

public sealed class RestoreMaterialCommandHandler(CatalogDbContext dbContext, ICatalogHistoryWriter historyWriter)
    : ICommandHandler<RestoreMaterialCommand, RestoreMaterialCommandResult>
{
    public async Task<RestoreMaterialCommandResult> Handle(RestoreMaterialCommand command, CancellationToken ct)
    {
        return await CatalogOrderTransaction.Run(dbContext, async () =>
        {
            var source = await dbContext.ArchivedMaterials
                .SingleOrDefaultAsync(material => material.Id == command.Id, ct);
            var originalMaterialId = source?.OriginalMaterialId ?? command.Id;
            if (source is null)
            {
                if (await dbContext.Materials.AsNoTracking().AnyAsync(material => material.Id == originalMaterialId, ct))
                    return new RestoreMaterialCommandResult(true);
                throw new BadHttpRequestException("Материал не найден.");
            }
            if (await dbContext.Materials.AsNoTracking().AnyAsync(material => material.Id == originalMaterialId, ct))
                throw new BadHttpRequestException("Материал уже существует в целевом списке.");
            var position = source.OriginalOrderByCol;
            var materials = await dbContext.Materials.OrderBy(material => material.OrderByCol).ToArrayAsync(ct);
            if (materials.Any(material => material.OrderByCol == position))
            {
                var following = materials.Where(material => material.OrderByCol >= position)
                    .Select(material => new { Material = material, Position = material.OrderByCol }).ToArray();
                if (following[^1].Position == int.MaxValue)
                    throw new BadHttpRequestException("Достигнут предел порядка материалов. Освободите место перед восстановлением.");

                var usedPositions = materials.Select(material => material.OrderByCol)
                    .Concat(following.Select(item => item.Position + 1)).Append(position).ToHashSet();
                var temporaryPosition = 0;
                foreach (var item in following)
                {
                    while (!usedPositions.Add(temporaryPosition))
                    {
                        if (temporaryPosition == int.MinValue)
                            throw new BadHttpRequestException("Нет свободных позиций для восстановления материала.");
                        temporaryPosition--;
                    }
                    item.Material.OrderByCol = temporaryPosition;
                }
                await dbContext.SaveChangesAsync(ct);

                foreach (var item in following)
                    item.Material.OrderByCol = item.Position + 1;
                await dbContext.SaveChangesAsync(ct);
            }
            var destination = new Material
            {
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
                OrderByCol = position
            };
            dbContext.Materials.Add(destination);
            dbContext.Entry(destination).Property(material => material.Id).CurrentValue = source.OriginalMaterialId;
            dbContext.Entry(destination).Property(material => material.Id).IsTemporary = false;
            await dbContext.SaveChangesAsync(ct);

            // Existing child rows keep their identity; only their material owner changes.
            // The source is tracked without children, so deleting it cannot orphan the moved rows in the change tracker.
            var imageCount = await dbContext.MaterialImages.Where(image => image.ArchivedMaterialId == source.Id)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(image => image.MaterialId, destination.Id)
                    .SetProperty(image => image.ArchivedMaterialId, (int?)null), ct);
            var linkCount = await dbContext.MaterialEdgeLinks.Where(link => link.ArchivedMaterialId == source.Id)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(link => link.MaterialId, destination.Id)
                    .SetProperty(link => link.ArchivedMaterialId, (int?)null), ct);
            dbContext.ArchivedMaterials.Remove(source);
            historyWriter.Add(CatalogHistoryActionTypeEnum.Update, CatalogHistoryEntityTypeEnum.Material, destination.Id,
                $"Материал #{destination.Id} «{destination.Name}» восстановлен из архива: изображений {imageCount}, связей с кромками {linkCount}.");
            await dbContext.SaveChangesAsync(ct);
            return new RestoreMaterialCommandResult(true);
        }, ct);
    }
}