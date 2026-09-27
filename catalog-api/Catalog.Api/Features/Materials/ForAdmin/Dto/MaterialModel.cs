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
        RuleFor(m => m.Price)
            .NotNull().WithMessage("Не указана стоимость материала")
            .GreaterThan(0).When(m => m.Count > 0).WithMessage("Стоимость материала должна быть больше 0 при количестве больше 0");
        RuleFor(m => m.CategoryId)
            .Cascade(CascadeMode.Stop)
            .GreaterThan(0).WithMessage("Не указана категория(коллекция) материала")
            .MustAsync(async (categoryId, ct) =>
                await dbContext.MaterialCategories.AnyAsync(x => x.Id == categoryId, ct))
            .WithMessage("Указанная категория материалов не найдена");
        RuleFor(m => m.SheetSizeId)
            .Cascade(CascadeMode.Stop)
            .GreaterThan(0).WithMessage("Не указан размер материала")
            .MustAsync(async (sheetSizeId, ct) =>
                await dbContext.MaterialSheetSizes.AnyAsync(x => x.Id == sheetSizeId, ct))
            .WithMessage("Указанный размер материала не найден");
        RuleFor(m => m.ManufacturerId)
            .Cascade(CascadeMode.Stop)
            .GreaterThan(0).WithMessage("Не указан производитель")
            .MustAsync(async (manufacturerId, ct) =>
                await dbContext.MaterialManufacturers.AnyAsync(x => x.Id == manufacturerId, ct))
            .WithMessage("Указанный производитель не найден");
        RuleFor(m => m.KvM).GreaterThan(0).WithMessage("Полезная площадь материала должна быть больше нуля");
        RuleFor(m => m.Depth).GreaterThan(0).WithMessage("Толщина материала должна быть больше нуля");
        RuleFor(m => m.PerimetrM).GreaterThan(0).WithMessage("Периметр материала должен быть больше нуля");
        RuleFor(m => m).Must(x => x.ApplicableToPvhFacades || x.ApplicableToEmalFacades || x.ApplicableToRaskroys)
            .WithMessage("Материал должен быть применен хотя бы к одному калькулятору");
        RuleFor(m => m.Image)
            .MustAsync(async (imageGuid, ct) =>
                imageGuid is null
                || await dbContext.ImageCache.AnyAsync(x => x.Guid == imageGuid, ct)
                || await dbContext.MaterialImages.AnyAsync(x => x.Guid == imageGuid, ct))
            .WithMessage("Изображение с указанным GUID не найдено");
    }
}

public sealed record MaterialModel
{
    [Description("Id категории")]
    public int CategoryId { get; init; }

    [Description("Название материала")]
    public required string Name { get; init; }

    [Description("Артикул материала")]
    public required string Article { get; init; }

    [Description("Изображение материала")]
    public Guid? Image { get; init; }

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

    [Description("Единица измерения")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public CountTypeEnum CountTypeEnum { get; init; }
}