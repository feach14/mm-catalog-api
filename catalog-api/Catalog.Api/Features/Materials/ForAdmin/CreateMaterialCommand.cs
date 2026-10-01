using Catalog.Api.Features.Materials.ForAdmin.Dto;
using Catalog.Api.Services.History;
using Catalog.Database;
using Catalog.Database.Entities;
using Catalog.Database.Enums;
using Core.CQRS;

namespace Catalog.Api.Features.Materials.ForAdmin;

public sealed record CreateMaterialCommand(CreateMaterialModel Material)
    : ICommand<CreateMaterialCommandResult>;

public sealed record CreateMaterialModel : MaterialModel;

public sealed class CreateMaterialModelValidator : AbstractValidator<CreateMaterialModel>
{
    public CreateMaterialModelValidator(CatalogDbContext dbContext)
    {
        Include(new MaterialModelValidator(dbContext));

        RuleFor(x => x.Name)
            .MustAsync(async (name, ct) => !await dbContext.Materials.AnyAsync(x => x.Name == name.Trim(), ct))
            .When(x => !string.IsNullOrWhiteSpace(x.Name))
            .WithMessage("Материал с таким названием уже существует");
        RuleFor(x => x.Image)
            .MustAsync(async (imageGuid, ct) =>
                await dbContext.ImageCache.AnyAsync(image => image.Guid == imageGuid && image.ImageType == MaterialImageTypeEnum.Original, ct))
            .When(x => x.Image is not null)
            .WithMessage("Оригинальное изображение с указанным GUID не найдено или имеет другое назначение");
        RuleFor(x => x.Thumbnail240)
            .MustAsync(async (imageGuid, ct) =>
                await dbContext.ImageCache.AnyAsync(image => image.Guid == imageGuid && image.ImageType == MaterialImageTypeEnum.Thumbnail240, ct))
            .When(x => x.Thumbnail240 is not null)
            .WithMessage("Миниатюра 240 с указанным GUID не найдена или имеет другое назначение");
        RuleFor(x => x.Thumbnail480)
            .MustAsync(async (imageGuid, ct) =>
                await dbContext.ImageCache.AnyAsync(image => image.Guid == imageGuid && image.ImageType == MaterialImageTypeEnum.Thumbnail480, ct))
            .When(x => x.Thumbnail480 is not null)
            .WithMessage("Миниатюра 480 с указанным GUID не найдена или имеет другое назначение");
    }
}

public sealed record CreateMaterialCommandResult([property: Description("Id материала")] int Id);

public class CreateMaterialCommandHandler(CatalogDbContext dbContext, ICatalogHistoryWriter historyWriter) : ICommandHandler<CreateMaterialCommand, CreateMaterialCommandResult>
{
    public async Task<CreateMaterialCommandResult> Handle(CreateMaterialCommand command, CancellationToken ct)
    {
        var maxOrderByCol = await dbContext.Materials.MaxAsync(x => (int?)x.OrderByCol, ct) ?? 0;

        var material = new Material
        {
            CategoryId = command.Material.CategoryId,
            MaterialSheetSizeId = command.Material.SheetSizeId,
            MaterialManufacturerId = command.Material.ManufacturerId,
            MaterialTypeId = command.Material.MaterialTypeId,
            Article = command.Material.Article,
            Name = command.Material.Name,
            MaterialThicknessId = command.Material.ThicknessId,
            KvM = command.Material.KvM,
            PerimetrM = command.Material.PerimetrM,
            Count = command.Material.Count,
            ApplicableToRaskroys = command.Material.ApplicableToRaskroys,
            ApplicableToPvhFacades = command.Material.ApplicableToPvhFacades,
            ApplicableToEmalFacades = command.Material.ApplicableToEmalFacades,
            CommentOnMaterialIsRequired = command.Material.CommentOnMaterialIsRequired,
            AllowSecondItemInOrder = command.Material.AllowSecondItemInOrder,
            ExternalLink = command.Material.ExternalLink,
            Price = command.Material.Price,
            HideOnSite = command.Material.HideOnSite,
            HidePriceOnSite = command.Material.HidePriceOnSite,
            CountTypeEnum = command.Material.CountTypeEnum,
            OrderByCol = maxOrderByCol + 1
        };

        await using var transaction = await dbContext.Database.BeginTransactionAsync(ct);

        await dbContext.Materials.AddAsync(material, ct);
        await dbContext.SaveChangesAsync(ct);

        Guid?[] images = [command.Material.Image, command.Material.Thumbnail480, command.Material.Thumbnail240];
        foreach (var imageGuid in images.Where(x => x != null))
        {
            var fileFromImgCache = await dbContext.ImageCache.SingleAsync(x => x.Guid == imageGuid, ct);
            await dbContext.MaterialImages.AddAsync(new MaterialImage
            {
                Data = fileFromImgCache.Data,
                Guid = fileFromImgCache.Guid,
                MaterialId = material.Id,
                Type = fileFromImgCache.Type,
                ImageType = fileFromImgCache.ImageType
            }, ct);

            dbContext.ImageCache.RemoveRange(fileFromImgCache);
        }

        historyWriter.Add(
            CatalogHistoryActionTypeEnum.Create,
            CatalogHistoryEntityTypeEnum.Material,
            material.Id,
            $"Создан материал #{material.Id} «{material.Name}»: "
            + $"артикул {material.Article}, "
            + $"категория #{material.CategoryId}, "
            + $"производитель #{material.MaterialManufacturerId}, тип материала #{material.MaterialTypeId}, "
            + $"размер #{material.MaterialSheetSizeId}, "
            + $"толщина #{material.MaterialThicknessId}, "
            + $"количество {material.Count}, "
            + $"цена {material.Price}, "
            + $"скрыт на сайте: {material.HideOnSite}, цена скрыта на сайте: {material.HidePriceOnSite}, "
            + $"изображения {string.Join(';', images.Select(x => $"{(x?.ToString() ?? "нет")}"))}.");

        await dbContext.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        return new CreateMaterialCommandResult(material.Id);
    }
}