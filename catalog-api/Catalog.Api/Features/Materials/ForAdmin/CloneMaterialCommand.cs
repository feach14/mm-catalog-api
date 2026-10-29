using Catalog.Api.Infrastructure.Ordering;
using Catalog.Api.Services.History;
using Catalog.Database;
using Catalog.Database.Entities;
using Catalog.Database.Enums;
using Core.CQRS;

namespace Catalog.Api.Features.Materials.ForAdmin;

public sealed record CloneMaterialCommand(int Id) : ICommand<CloneMaterialCommandResult>;

public sealed record CloneMaterialCommandResult(
    [property: Description("Id копии материала")] int NewMaterialId);

public sealed class CloneMaterialCommandHandler(CatalogDbContext dbContext, ICatalogHistoryWriter historyWriter)
    : ICommandHandler<CloneMaterialCommand, CloneMaterialCommandResult>
{
    public async Task<CloneMaterialCommandResult> Handle(CloneMaterialCommand command, CancellationToken ct)
    {
        return await CatalogOrderTransaction.Run(dbContext, async () =>
        {
            var materials = await dbContext.Materials.OrderBy(material => material.OrderByCol).ToArrayAsync(ct);
            var sourceIndex = Array.FindIndex(materials, material => material.Id == command.Id);
            if (sourceIndex < 0)
                throw new BadHttpRequestException("Материал не найден.");
            var source = materials[sourceIndex];
            var name = $"{source.Name} (Копия)";
            if (name.Length > 250)
                throw new BadHttpRequestException("Название копии превышает 250 символов. Сократите название исходного материала.");
            if (materials[^1].OrderByCol == int.MaxValue)
                throw new BadHttpRequestException("Достигнут предел порядка материалов. Упорядочьте существующие записи перед копированием.");

            var sourceImages = await dbContext.MaterialImages.AsNoTracking()
                .Where(image => image.MaterialId == source.Id).ToArrayAsync(ct);
            var sourceLinks = await dbContext.MaterialEdgeLinks.AsNoTracking().Include(link => link.Markings)
                .Where(link => link.MaterialId == source.Id).ToArrayAsync(ct);

            var clonePosition = source.OrderByCol + 1;
            var following = materials.Skip(sourceIndex + 1)
                .Select(material => new { Material = material, Position = material.OrderByCol + 1 }).ToArray();
            if (following.Length > 0)
            {
                // Temporary positions stay outside both occupied and final positions, so neither save can collide with the unique index.
                var usedPositions = materials.Select(material => material.OrderByCol)
                    .Concat(following.Select(item => item.Position)).Append(clonePosition).ToHashSet();
                var temporaryPosition = 0;
                foreach (var item in following)
                {
                    while (!usedPositions.Add(temporaryPosition))
                    {
                        if (temporaryPosition == int.MinValue)
                            throw new BadHttpRequestException("Нет свободных позиций для сортировки материалов.");
                        temporaryPosition--;
                    }
                    item.Material.OrderByCol = temporaryPosition;
                }
                await dbContext.SaveChangesAsync(ct);

                foreach (var item in following)
                    item.Material.OrderByCol = item.Position;
                await dbContext.SaveChangesAsync(ct);
            }

            var clone = new Material
            {
                CategoryId = source.CategoryId,
                MaterialSheetSizeId = source.MaterialSheetSizeId,
                MaterialTypeId = source.MaterialTypeId,
                MaterialManufacturerId = source.MaterialManufacturerId,
                Name = name,
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
                HidePriceOnSite = source.HidePriceOnSite,
                CountTypeEnum = source.CountTypeEnum,
                OrderByCol = clonePosition,
                ApplicableToCutting = source.ApplicableToCutting,
                ApplicableToPvhFacades = source.ApplicableToPvhFacades,
                ApplicableToEnamelFacades = source.ApplicableToEnamelFacades
            };
            dbContext.Materials.Add(clone);
            await dbContext.SaveChangesAsync(ct);

            foreach (var image in sourceImages)
                dbContext.MaterialImages.Add(new MaterialImage
                {
                    MaterialId = clone.Id,
                    Guid = Guid.NewGuid(),
                    ContentType = image.ContentType,
                    Data = [.. image.Data],
                    ImageType = image.ImageType
                });

            foreach (var sourceLink in sourceLinks)
            {
                var link = new MaterialEdgeLink
                {
                    MaterialId = clone.Id,
                    EdgeId = sourceLink.EdgeId,
                    Priority = sourceLink.Priority
                };
                foreach (var marking in sourceLink.Markings)
                    link.Markings.Add(new MaterialEdgeLinkMarking
                    {
                        MaterialId = clone.Id,
                        EdgeId = link.EdgeId,
                        MarkingId = marking.MarkingId
                    });
                dbContext.MaterialEdgeLinks.Add(link);
            }

            historyWriter.Add(CatalogHistoryActionTypeEnum.Create, CatalogHistoryEntityTypeEnum.Material, clone.Id,
                $"Создан материал #{clone.Id} «{clone.Name}» копированием материала #{source.Id} «{source.Name}»: "
                + $"изображений {sourceImages.Length}, связей с кромками {sourceLinks.Length}.");
            await dbContext.SaveChangesAsync(ct);
            return new CloneMaterialCommandResult(clone.Id);
        }, ct);
    }
}