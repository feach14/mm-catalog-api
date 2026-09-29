// ReSharper disable UnusedAutoPropertyAccessor.Global

using Catalog.Database;
using Catalog.Database.Enums;

namespace Catalog.Api.Features.Materials.ForAdmin.Dto;

public class MaterialModelValidator : AbstractValidator<MaterialModel>
{
    public MaterialModelValidator(CatalogDbContext dbContext)
    {
        RuleFor(m => m.Name).NotEmpty().WithMessage("Не указано название материала");
        RuleFor(m => m.Article).NotEmpty().WithMessage("Не указан артикул материала");
        RuleFor(m => m.Price).GreaterThan(0).When(m => m.Count > 0).WithMessage("Стоимость материала должна быть больше 0 при количестве больше 0");
        RuleFor(m => m.CategoryId)
            .Cascade(CascadeMode.Stop)
            .GreaterThan(0).WithMessage("Не указана категория(коллекция) материала")
            .MustAsync(async (categoryId, ct) => await dbContext.MaterialCategories.AnyAsync(x => x.Id == categoryId, ct))
            .WithMessage("Указанная категория материалов не найдена");
        RuleFor(m => m.SheetSizeId)
            .Cascade(CascadeMode.Stop)
            .GreaterThan(0).WithMessage("Не указан размер материала")
            .MustAsync(async (sheetSizeId, ct) => await dbContext.MaterialSheetSizes.AnyAsync(x => x.Id == sheetSizeId, ct))
            .WithMessage("Указанный размер материала не найден");
        RuleFor(m => m.ManufacturerId)
            .Cascade(CascadeMode.Stop)
            .GreaterThan(0).WithMessage("Не указан производитель")
            .MustAsync(async (manufacturerId, ct) => await dbContext.MaterialManufacturers.AnyAsync(x => x.Id == manufacturerId, ct))
            .WithMessage("Указанный производитель не найден");
        RuleFor(m => m.KvM).GreaterThan(0).WithMessage("Полезная площадь материала должна быть больше нуля");
        RuleFor(m => m.Depth).GreaterThan(0).WithMessage("Толщина материала должна быть больше нуля");
        RuleFor(m => m.PerimetrM).GreaterThan(0).WithMessage("Периметр материала должен быть больше нуля");
        RuleFor(m => m.ApplicableToRaskroys)
            .Must((model, applicableToRaskroys) =>
                applicableToRaskroys || model.ApplicableToPvhFacades || model.ApplicableToEmalFacades)
            .WithMessage("Материал должен быть применен хотя бы к одному калькулятору");
        RuleFor(m => m.Image)
            .MustAsync(async (imageGuid, ct) => await ImageExists(dbContext, imageGuid, MaterialImageTypeEnum.Original, ct))
            .When(m => m.Image is not null)
            .WithMessage("Оригинальное изображение с указанным GUID не найдено или имеет другое назначение");
        RuleFor(m => m.Thumbnail240)
            .MustAsync(async (imageGuid, ct) => await ImageExists( dbContext, imageGuid, MaterialImageTypeEnum.Thumbnail240, ct))
            .When(m => m.Thumbnail240 is not null)
            .WithMessage("Миниатюра 240 с указанным GUID не найдена или имеет другое назначение");
        RuleFor(m => m.Thumbnail480)
            .MustAsync(async (imageGuid, ct) => await ImageExists(dbContext, imageGuid, MaterialImageTypeEnum.Thumbnail480, ct))
            .When(m => m.Thumbnail480 is not null)
            .WithMessage("Миниатюра 480 с указанным GUID не найдена или имеет другое назначение");
    }

    private static async Task<bool> ImageExists(
        CatalogDbContext dbContext,
        Guid? imageGuid,
        MaterialImageTypeEnum imageType,
        CancellationToken ct) =>
        imageGuid.HasValue
        && (await dbContext.ImageCache.AnyAsync(
                x => x.Guid == imageGuid && x.ImageType == imageType,
                ct)
            || await dbContext.MaterialImages.AnyAsync(
                x => x.Guid == imageGuid && x.ImageType == imageType,
                ct));
}

public record MaterialModel
{
    [Description("Id категории")]
    public int CategoryId { get; init; }

    [Description("Название материала")]
    public required string Name { get; init; }

    [Description("Артикул материала")]
    public required string Article { get; init; }

    [Description("GUID оригинального изображения")]
    public Guid? Image { get; init; }

    [Description("GUID квадратной миниатюры 240 на 240 пикселей")]
    public Guid? Thumbnail240 { get; init; }

    [Description("GUID квадратной миниатюры 480 на 480 пикселей")]
    public Guid? Thumbnail480 { get; init; }

    [Description("Количество")]
    public int Count { get; init; }

    [Description("Id размера материала")]
    public int SheetSizeId { get; init; }

    [Description("Id производителя")]
    public int ManufacturerId { get; init; }

    [Description("Толщина материала")]
    public double Depth { get; init; }

    [Description("Количество кв.м. в плите материала")]
    public double KvM { get; init; }

    [Description("Количество метров по периметру плиты")]
    public double PerimetrM { get; init; }

    [Description("Признак: Материал применим в калькуляторе раскроя")]
    public bool ApplicableToRaskroys { get; init; }

    [Description("Признак: Материал применим в калькуляторе фасадов ПВХ")]
    public bool ApplicableToPvhFacades { get; init; }

    [Description("Признак: Материал применим в калькуляторе фасадов эмаль")]
    public bool ApplicableToEmalFacades { get; init; }

    [Description("Признак: Комментарий к материалу обязателен при оформлении заявки(расчета)")]
    public bool CommentOnMaterialIsRequired { get; init; }

    [Description("Признак: Разрешено добавлять вторым(и более) элементом списка расчетов в заявке")]
    public bool AllowSecondItemInOrder { get; init; }

    [Description("Ссылка на внешний источник")]
    public string? ExternalLink { get; init; }

    [Description("Цена материала у поставщика")]
    public decimal Price { get; init; }

    [Description("Единица измерения"), JsonConverter(typeof(JsonStringEnumConverter))]
    public CountTypeEnum CountTypeEnum { get; init; }
}
