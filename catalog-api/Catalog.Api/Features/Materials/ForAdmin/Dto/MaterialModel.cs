// ReSharper disable UnusedAutoPropertyAccessor.Global

using Catalog.Api.Services.Stroydvor;
using Catalog.Database;
using Core.BaseEnums;

namespace Catalog.Api.Features.Materials.ForAdmin.Dto;

public class MaterialModelValidator : AbstractValidator<MaterialModel>
{
    public MaterialModelValidator(CatalogDbContext dbContext, IHttpContextAccessor httpContextAccessor)
    {
        RuleFor(m => m.Name).NotEmpty().WithMessage("Не указано название материала");
        RuleFor(m => m.Article).NotEmpty().WithMessage("Не указан артикул материала");
        RuleFor(m => m.Price).GreaterThan(0).When(m => m.Count > 0).WithMessage("Стоимость материала должна быть больше 0 при количестве больше 0");
        RuleFor(m => m.StroydvorProductId).GreaterThan(0)
            .When(m => m.StroydvorProductId.HasValue)
            .WithMessage("Id товара Стройдвора должен быть положительным числом");
        RuleFor(m => m).MustAsync(async (model, ct) =>
            {
                if (string.IsNullOrWhiteSpace(model.Name))
                    return true;

                var productId = model.StroydvorProductId;
                if (!model.StroydvorProductIdSpecified
                    && int.TryParse(httpContextAccessor.HttpContext?.Request.RouteValues["id"]?.ToString(), out var materialId))
                    productId = await dbContext.Materials.AsNoTracking().Where(x => x.Id == materialId)
                        .Select(x => x.StroydvorProductId).SingleOrDefaultAsync(ct);
                if (!productId.HasValue)
                    return true;

                var thickness = await dbContext.MaterialThicknesses.AsNoTracking()
                    .Where(x => x.Id == model.ThicknessId).Select(x => (decimal?)x.Value).SingleOrDefaultAsync(ct);
                var size = await dbContext.MaterialSheetSizes.AsNoTracking()
                    .Where(x => x.Id == model.SheetSizeId).Select(x => new { x.Height, x.Width }).SingleOrDefaultAsync(ct);
                if (!thickness.HasValue || size is null)
                    return true;

                return StroydvorMaterialScope.IsEligible(model.CategoryId, model.ManufacturerId, model.MaterialTypeId,
                    thickness.Value, size.Height, size.Width, model.Name);
            })
            .WithMessage("Привязка к Стройдвору разрешена только для ЛДСП 16 мм выбранных производителей и форматов. Для другого материала удалите Id товара Стройдвора.");
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
        RuleFor(m => m.ThicknessId)
            .Cascade(CascadeMode.Stop)
            .GreaterThan(0).WithMessage("Не указана толщина материала")
            .MustAsync(async (id, ct) => await dbContext.MaterialThicknesses.AsNoTracking().AnyAsync(x => x.Id == id, ct))
            .WithMessage("Указанная толщина материала не найдена");
    }
}

public record MaterialModel
{
    private int? _stroydvorProductId;

    internal bool StroydvorProductIdSpecified { get; private set; }

    [Description("Id товара Стройдвора; null — удалить привязку")]
    public int? StroydvorProductId
    {
        get => _stroydvorProductId;
        init
        {
            _stroydvorProductId = value;
            StroydvorProductIdSpecified = true;
        }
    }

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

    [Description("Признак: Комментарий к материалу обязателен при оформлении заявки(расчета)")]
    public bool CommentOnMaterialIsRequired { get; init; }

    [Description("Признак: Разрешено добавлять вторым(и более) элементом списка расчетов в заявке")]
    public bool AllowSecondItemInOrder { get; init; }

    [Description("Ссылка на внешний источник")]
    public string? ExternalLink { get; init; }

    [Description("Цена материала у поставщика")]
    public required decimal Price { get; init; }

    [Description("Скрыть в ЛК")]
    public bool HideInLk { get; init; }

    [Description("Признак: скрыть материал на сайте")]
    public bool HideOnSite { get; init; }

    [Description("Признак: не показывать цену материала на сайте")]
    public bool HidePriceOnSite { get; init; }

    [Description("Единица измерения"), JsonConverter(typeof(StrictStringEnumConverter))]
    public required CountTypeEnum CountTypeEnum { get; init; }
}