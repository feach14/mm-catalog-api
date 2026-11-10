using Catalog.Api.Features.Materials.ForAdmin.Dto;
using Catalog.Api.Services.History;
using Catalog.Database;
using Catalog.Database.Entities;
using Catalog.Database.Enums;
using Core.CQRS;
using Microsoft.Extensions.Caching.Memory;

namespace Catalog.Api.Features.Materials.ForAdmin;

public sealed record UpdateArchivedMaterialCommand(int Id, UpdateMaterialModel Material) : ICommand<UpdateMaterialCommandResult>;

public class UpdateArchivedMaterialCommandHandler(
    CatalogDbContext dbContext,
    IMemoryCache memoryCache,
    ICatalogHistoryWriter historyWriter) : ICommandHandler<UpdateArchivedMaterialCommand, UpdateMaterialCommandResult>
{
    public async Task<UpdateMaterialCommandResult> Handle(UpdateArchivedMaterialCommand command, CancellationToken ct)
    {
        var material = await dbContext.ArchivedMaterials
            .Include(x => x.Category)
            .Include(x => x.Images)
            .SingleAsync(x => x.Id == command.Id, ct);

        var original = material.Images.SingleOrDefault(x => x.ImageType == MaterialImageTypeEnum.Original);
        var thumbnail240 = material.Images.SingleOrDefault(x => x.ImageType == MaterialImageTypeEnum.Thumbnail240);
        var thumbnail480 = material.Images.SingleOrDefault(x => x.ImageType == MaterialImageTypeEnum.Thumbnail480);

        var changes = new List<string>();
        AddChange(changes, "категория", material.CategoryId, command.Material.CategoryId);
        AddChange(changes, "размер", material.MaterialSheetSizeId, command.Material.SheetSizeId);
        AddChange(changes, "производитель", material.MaterialManufacturerId, command.Material.ManufacturerId);
        AddChange(changes, "тип материала", material.MaterialTypeId, command.Material.MaterialTypeId);
        AddChange(changes, "название", material.Name, command.Material.Name, true);
        AddChange(changes, "артикул", material.Article, command.Material.Article, true);
        AddChange(changes, "толщина", material.MaterialThicknessId, command.Material.ThicknessId);
        AddChange(changes, "количество", material.Count, command.Material.Count);
        AddChange(changes, "ссылка", material.ExternalLink, command.Material.ExternalLink, true);
        AddChange(changes, "обязательный комментарий", material.CommentOnMaterialIsRequired, command.Material.CommentOnMaterialIsRequired);
        AddChange(changes, "второй элемент в заказе", material.AllowSecondItemInOrder, command.Material.AllowSecondItemInOrder);
        AddChange(changes, "цена", material.Price, command.Material.Price);
        if (command.Material.StroydvorProductIdSpecified)
            AddChange(changes, "Id товара Стройдвора", material.StroydvorProductId, command.Material.StroydvorProductId);
        AddChange(changes, "скрыт в ЛК", material.HideInLk, command.Material.HideInLk);
        AddChange(changes, "скрыт на сайте", material.HideOnSite, command.Material.HideOnSite);
        AddChange(changes, "цена скрыта на сайте", material.HidePriceOnSite, command.Material.HidePriceOnSite);
        AddChange(changes, "единица", material.CountTypeEnum, command.Material.CountTypeEnum);
        AddChange(changes, "оригинальное изображение", original?.Guid, command.Material.Image);
        AddChange(changes, "миниатюра 240", thumbnail240?.Guid, command.Material.Thumbnail240);
        AddChange(changes, "миниатюра 480", thumbnail480?.Guid, command.Material.Thumbnail480);

        if (changes.Count == 0)
            return new UpdateMaterialCommandResult(true);

        material.CategoryId = command.Material.CategoryId;
        material.MaterialSheetSizeId = command.Material.SheetSizeId;
        material.MaterialManufacturerId = command.Material.ManufacturerId;
        material.MaterialTypeId = command.Material.MaterialTypeId;
        material.Article = command.Material.Article;
        material.Name = command.Material.Name;
        material.MaterialThicknessId = command.Material.ThicknessId;
        material.Count = command.Material.Count;
        material.CommentOnMaterialIsRequired = command.Material.CommentOnMaterialIsRequired;
        material.AllowSecondItemInOrder = command.Material.AllowSecondItemInOrder;
        material.ExternalLink = command.Material.ExternalLink;
        material.Price = command.Material.Price;
        if (command.Material.StroydvorProductIdSpecified && material.StroydvorProductId != command.Material.StroydvorProductId)
        {
            material.StroydvorProductId = command.Material.StroydvorProductId;
            material.StroydvorArticle = null;
            material.StroydvorPrice = 0m;
            material.StroydvorLastSyncAt = null;
            material.StroydvorInStock = false;
        }
        material.HideInLk = command.Material.HideInLk;
        material.HideOnSite = command.Material.HideOnSite;
        material.HidePriceOnSite = command.Material.HidePriceOnSite;
        material.CountTypeEnum = command.Material.CountTypeEnum;

        Guid?[] replacedImageGuids =
        [
            await UpdateImage(material, original, command.Material.Image, MaterialImageTypeEnum.Original, ct),
            await UpdateImage(material, thumbnail240, command.Material.Thumbnail240, MaterialImageTypeEnum.Thumbnail240, ct),
            await UpdateImage(material, thumbnail480, command.Material.Thumbnail480, MaterialImageTypeEnum.Thumbnail480, ct)
        ];

        historyWriter.Add(
            CatalogHistoryActionTypeEnum.Update,
            CatalogHistoryEntityTypeEnum.Material,
            material.Id,
            $"Материал #{material.Id} «{material.Name}» изменён: {string.Join(", ", changes)}.");

        await dbContext.SaveChangesAsync(ct);

        foreach (var imageGuid in replacedImageGuids.Where(x => x.HasValue))
            memoryCache.Remove(imageGuid!.Value);

        return new UpdateMaterialCommandResult(true);
    }

    private async Task<Guid?> UpdateImage(
        ArchivedMaterial material,
        MaterialImage? currentImage,
        Guid? requestedGuid,
        MaterialImageTypeEnum imageType,
        CancellationToken ct)
    {
        if (currentImage?.Guid == requestedGuid)
            return null;

        if (currentImage is not null)
        {
            material.Images.Remove(currentImage);
            dbContext.MaterialImages.Remove(currentImage);
        }

        if (requestedGuid is null)
            return currentImage?.Guid;

        var cachedFile = await dbContext.ImageCache
            .SingleAsync(x => x.Guid == requestedGuid.Value && x.ImageType == imageType, ct);

        material.Images.Add(new MaterialImage
        {
            Data = cachedFile.Data,
            ContentType = cachedFile.ContentType,
            Guid = cachedFile.Guid,
            ArchivedMaterialId = material.Id,
            ImageType = imageType
        });
        dbContext.ImageCache.Remove(cachedFile);
        return currentImage?.Guid;
    }

    private static void AddChange<T>(List<string> changes, string name, T oldValue, T newValue, bool quote = false)
    {
        if (EqualityComparer<T>.Default.Equals(oldValue, newValue))
            return;
        var oldText = oldValue?.ToString() ?? "нет";
        var newText = newValue?.ToString() ?? "нет";
        changes.Add(quote
            ? $"{name} «{oldText}» → «{newText}»"
            : $"{name} {oldText} → {newText}");
    }
}