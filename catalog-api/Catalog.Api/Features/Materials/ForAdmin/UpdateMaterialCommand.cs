using Catalog.Api.Features.Materials.ForAdmin.Dto;
using Catalog.Api.Services.History;
using Catalog.Database;
using Catalog.Database.Entities;
using Catalog.Database.Enums;
using Core.CQRS;

namespace Catalog.Api.Features.Materials.ForAdmin;

public sealed record UpdateMaterialCommand(int Id, UpdateMaterialModel Material) : ICommand<UpdateMaterialCommandResult>;

public sealed record UpdateMaterialModel : MaterialModel;

public sealed class UpdateMaterialModelValidator : AbstractValidator<UpdateMaterialModel>
{
    public UpdateMaterialModelValidator(CatalogDbContext dbContext, IHttpContextAccessor httpContextAccessor)
    {
        Include(new MaterialModelValidator(dbContext));
        RuleFor(x => x)
            .MustAsync(async (_, ct) =>
            {
                var routeId = httpContextAccessor.HttpContext?.Request.RouteValues["id"]?.ToString();
                return int.TryParse(routeId, out var id)
                       && await dbContext.Materials.AnyAsync(x => x.Id == id, ct);
            })
            .WithMessage("Указанный материал не существует")
            .OverridePropertyName("Id");
        RuleFor(x => x.Name)
            .MustAsync(async (name, ct) =>
            {
                var routeId = httpContextAccessor.HttpContext?.Request.RouteValues["id"]?.ToString();
                return int.TryParse(routeId, out var id)
                       && !await dbContext.Materials.AnyAsync(
                           x => x.Id != id && x.Name == name.Trim(), ct);
            })
            .When(x => !string.IsNullOrWhiteSpace(x.Name))
            .WithMessage("Материал с таким названием уже существует");
    }
}

public sealed record UpdateMaterialCommandResult(
    [property: Description("Успех операции")] bool Success);

public class UpdateMaterialCommandHandler(CatalogDbContext dbContext, ICatalogHistoryWriter historyWriter) : ICommandHandler<UpdateMaterialCommand, UpdateMaterialCommandResult>
{
    public async Task<UpdateMaterialCommandResult> Handle(UpdateMaterialCommand command, CancellationToken ct)
    {
        var material = await dbContext.Materials
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
        AddChange(changes, "оригинальное изображение", original?.Guid, command.Material.Image);
        AddChange(changes, "миниатюра 240", thumbnail240?.Guid, command.Material.Thumbnail240);
        AddChange(changes, "миниатюра 480", thumbnail480?.Guid, command.Material.Thumbnail480);

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
        
        await UpdateImage(material, original, command.Material.Image, MaterialImageTypeEnum.Original, ct);
        await UpdateImage(material, thumbnail240, command.Material.Thumbnail240, MaterialImageTypeEnum.Thumbnail240, ct);
        await UpdateImage(material, thumbnail480, command.Material.Thumbnail480, MaterialImageTypeEnum.Thumbnail480, ct);

        dbContext.Materials.Update(material);
        historyWriter.Add(
            CatalogHistoryActionTypeEnum.Update,
            CatalogHistoryEntityTypeEnum.Material,
            material.Id,
            $"Материал #{material.Id} «{material.Name}» изменён: {string.Join(", ", changes)}.");

        await dbContext.SaveChangesAsync(ct);
        
        return new UpdateMaterialCommandResult(true);
    }

    private async Task UpdateImage(Material material, MaterialImage? currentImage, Guid? requestedGuid, MaterialImageTypeEnum imageType, CancellationToken ct)
    {
        if (currentImage?.Guid == requestedGuid)
            return;
        
        if (currentImage is not null)
            material.Images.Remove(currentImage);

        if (requestedGuid is null)
            return;

        var cachedFile = await dbContext.ImageCache.SingleAsync(x => x.Guid == requestedGuid.Value && x.ImageType == imageType, ct);

        material.Images.Add(new MaterialImage
        {
            Data = cachedFile.Data,
            Type = cachedFile.Type,
            Guid = cachedFile.Guid,
            MaterialId = material.Id,
            ImageType = imageType
        });
        dbContext.ImageCache.Remove(cachedFile);
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
