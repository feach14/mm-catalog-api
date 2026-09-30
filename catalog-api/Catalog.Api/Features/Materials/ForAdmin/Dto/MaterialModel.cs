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
        RuleFor(m => m.MaterialTypeId)
            .Cascade(CascadeMode.Stop)
            .GreaterThan(0).WithMessage("Не указан тип материала")
            .MustAsync(async (materialTypeId, ct) => await dbContext.MaterialTypes.AnyAsync(x => x.Id == materialTypeId, ct))
            .WithMessage("Указанный тип материала не найден");
        RuleFor(m => m.KvM)
            .Cascade(CascadeMode.Stop)
            .GreaterThan(0).WithMessage("Полезная площадь материала должна быть больше нуля")
            .PrecisionScale(18, 3, true).WithMessage("Площадь должна содержать не более 15 цифр до точки и трёх после точки");
        RuleFor(m => m.PerimetrM)
            .PrecisionScale(18, 3, true).WithMessage("Периметр должен содержать не более 15 цифр до точки и трёх после точки");
        RuleFor(m => m.ThicknessId)
            .Cascade(CascadeMode.Stop)
            .GreaterThan(0).WithMessage("Не указана толщина материала")
            .MustAsync(async (id, ct) => await dbContext.MaterialThicknesses.AsNoTracking().AnyAsync(x => x.Id == id, ct))
            .WithMessage("Указанная толщина материала не найдена");
        RuleFor(m => m.PerimetrM).GreaterThan(0).WithMessage("Периметр материала должен быть больше нуля");
        RuleFor(m => m.ApplicableToRaskroys)
            .Must((model, applicableToRaskroys) =>
                applicableToRaskroys || model.ApplicableToPvhFacades || model.ApplicableToEmalFacades)
            .WithMessage("Материал должен быть применен хотя бы к одному калькулятору");
    }
}

public record MaterialModel
{
    [Description("Id категории")]
    public required int CategoryId { get; init; }

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
    public required int Count { get; init; }

    [Description("Id размера материала")]
    public required int SheetSizeId { get; init; }

    [Description("Id типа материала")]
    public required int MaterialTypeId { get; init; }

    [Description("Id производителя")]
    public required int ManufacturerId { get; init; }

    [Description("Id толщины материала")]
    public required int ThicknessId { get; init; }

    [Description("Количество кв.м. в плите материала, не более трёх знаков после десятичной точки")]
    public required decimal KvM { get; init; }

    [Description("Количество метров по периметру плиты, не более трёх знаков после десятичной точки")]
    public required decimal PerimetrM { get; init; }

    [Description("Признак: Материал применим в калькуляторе раскроя")]
    public required bool ApplicableToRaskroys { get; init; }

    [Description("Признак: Материал применим в калькуляторе фасадов ПВХ")]
    public required bool ApplicableToPvhFacades { get; init; }

    [Description("Признак: Материал применим в калькуляторе фасадов эмаль")]
    public required bool ApplicableToEmalFacades { get; init; }

    [Description("Признак: Комментарий к материалу обязателен при оформлении заявки(расчета)")]
    public bool CommentOnMaterialIsRequired { get; init; }

    [Description("Признак: Разрешено добавлять вторым(и более) элементом списка расчетов в заявке")]
    public bool AllowSecondItemInOrder { get; init; }

    [Description("Ссылка на внешний источник")]
    public string? ExternalLink { get; init; }

    [Description("Цена материала у поставщика")]
    public required decimal Price { get; init; }

    [Description("Единица измерения"), JsonConverter(typeof(JsonStringEnumConverter))]
    public required CountTypeEnum CountTypeEnum { get; init; }
}