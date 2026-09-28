using Catalog.Api.Features.History;
using Catalog.Api.Features.Materials.ForAdmin.Dto;
using Catalog.Database;
using Catalog.Database.Entities;
using Catalog.Database.Enums;
using Core.CQRS;
using Microsoft.Extensions.Caching.Memory;

namespace Catalog.Api.Features.Materials.ForAdmin;

public sealed record UpdateMaterialCommand(int Id, MaterialModel Material) : ICommand<UpdateMaterialCommandResult>;

public sealed record UpdateMaterialCommandResult(
    [property: Description("Успех операции")] bool Success);

public class UpdateMaterialCommandHandler(CatalogDbContext dbContext, IMemoryCache memoryCache, ICatalogHistoryWriter historyWriter) : ICommandHandler<UpdateMaterialCommand, UpdateMaterialCommandResult>
{
    public async Task<UpdateMaterialCommandResult> Handle(UpdateMaterialCommand command, CancellationToken ct)
    {
        var material =
            await dbContext.Materials
                .Include(x => x.Category)
                .Include(x => x.Images)
                .FirstOrDefaultAsync(x => x.Id == command.Id, ct)
            ?? throw new BadHttpRequestException($"Материал с id={command.Id} не существует.");

        var materialName = command.Material.Name.Trim();
        if (await dbContext.Materials.AnyAsync(x => x.Name == materialName && x.Id != command.Id, ct))
            throw new BadHttpRequestException("Материал с таким названием уже существует.");

        var oldImage = material.Images.Select(x => (Guid?)x.Guid).SingleOrDefault();
        var changes = new List<string>();
        AddChange(changes, "категория", material.CategoryId, command.Material.CategoryId);
        AddChange(changes, "размер", material.MaterialSheetSizeId, command.Material.SheetSizeId);
        AddChange(changes, "производитель", material.MaterialManufacturerId, command.Material.ManufacturerId);
        AddChange(changes, "название", material.Name, command.Material.Name, true);
        AddChange(changes, "артикул", material.Article, command.Material.Article, true);
        AddChange(changes, "толщина", material.Depth, command.Material.Depth);
        AddChange(changes, "площадь", material.KvM, command.Material.KvM);
        AddChange(changes, "периметр", material.PerimetrM, command.Material.PerimetrM);
        AddChange(changes, "количество", material.Count, command.Material.Count);
        AddChange(changes, "ссылка", material.ExternalLink, command.Material.ExternalLink, true);
        AddChange(changes, "раскрой", material.ApplicableToRaskroys, command.Material.ApplicableToRaskroys);
        AddChange(changes, "фасады ПВХ", material.ApplicableToPvhFacades, command.Material.ApplicableToPvhFacades);
        AddChange(changes, "фасады эмаль", material.ApplicableToEmalFacades, command.Material.ApplicableToEmalFacades);
        AddChange(changes, "обязательный комментарий", material.CommentOnMaterialIsRequired, command.Material.CommentOnMaterialIsRequired);
        AddChange(changes, "второй элемент в заказе", material.AllowSecondItemInOrder, command.Material.AllowSecondItemInOrder);
        AddChange(changes, "цена", material.Price, command.Material.Price);
        AddChange(changes, "единица", material.CountTypeEnum, command.Material.CountTypeEnum);
        AddChange(changes, "изображение", oldImage, command.Material.Image);

        if (changes.Count == 0)
            return new UpdateMaterialCommandResult(true);

        material.CategoryId = command.Material.CategoryId;
        material.MaterialSheetSizeId = command.Material.SheetSizeId;
        material.MaterialManufacturerId = command.Material.ManufacturerId;
        material.Article = command.Material.Article;
        material.Name = command.Material.Name;
        material.Depth = command.Material.Depth;
        material.KvM = command.Material.KvM;
        material.PerimetrM = command.Material.PerimetrM;
        material.Count = command.Material.Count;
        material.ApplicableToRaskroys = command.Material.ApplicableToRaskroys;
        material.ApplicableToPvhFacades = command.Material.ApplicableToPvhFacades;
        material.ApplicableToEmalFacades = command.Material.ApplicableToEmalFacades;
        material.CommentOnMaterialIsRequired = command.Material.CommentOnMaterialIsRequired;
        material.AllowSecondItemInOrder = command.Material.AllowSecondItemInOrder;
        material.ExternalLink = command.Material.ExternalLink;
        material.Price = command.Material.Price;
        material.CountTypeEnum = command.Material.CountTypeEnum;

        // Удаляем старые изображения
        var removedImages = material.Images
            .Where(img => command.Material.Image != img.Guid)
            .ToList();
        removedImages.ForEach(removeImg => material.Images.Remove(removeImg));

        GetMaterialImageQueryResult? imageToCache = null;

        // Сохраняем новые изображения
        if (command.Material.Image != null && material.Images.All(x => x.Guid != command.Material.Image))
        {
            var cachedFile = await dbContext.ImageCache
                .FirstOrDefaultAsync(x => x.Guid == command.Material.Image, ct);
            if (cachedFile is null)
                throw new BadHttpRequestException($"Файл {command.Material.Image} отсутствует в кэше");

            material.Images.Add(new MaterialImage
            {
                Data = cachedFile.Data,
                Type = cachedFile.Type,
                Guid = command.Material.Image.Value,
                MaterialId = material.Id
            });

            dbContext.ImageCache.Remove(cachedFile);
            imageToCache = new GetMaterialImageQueryResult(cachedFile.Data, cachedFile.Type);
        }

        dbContext.Materials.Update(material);
        historyWriter.Add(
            CatalogHistoryActionType.Update,
            CatalogHistoryEntityType.Material,
            material.Id,
            $"Материал #{material.Id} «{material.Name}» изменён: {string.Join(", ", changes)}.");

        await dbContext.SaveChangesAsync(ct);

        removedImages.ForEach(image => memoryCache.Remove(image.Guid));
        if (command.Material.Image != null && imageToCache != null)
            memoryCache.Set(command.Material.Image.Value, imageToCache);

        return new UpdateMaterialCommandResult(true);
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