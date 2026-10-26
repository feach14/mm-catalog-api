using Catalog.Database;

namespace Catalog.Api.Features.Edges.ForAdmin.Dto;

public record EdgeModel
{
    [Description("Название")]
    public required string Name { get; init; }

    [Description("Артикул")]
    public required string Article { get; init; }

    [Description("Стоимость за погонный метр, руб.")]
    public required decimal Price { get; init; }

    [Description("Id производителя")]
    public required int ManufacturerId { get; init; }

    [Description("Id толщины")]
    public required int ThicknessId { get; init; }

    [Description("Id единицы продажи")]
    public required int SaleUnitId { get; init; }

    [Description("Складская позиция")]
    public required bool IsStockItem { get; init; }

    [Description("Остаток в метрах; null для позиции под заказ")]
    public decimal? StockQuantity { get; init; }

    [Description("GUID оригинального изображения")]
    public Guid? Image { get; init; }

    [Description("GUID миниатюры")]
    public Guid? Thumbnail240 { get; init; }
}

public sealed class EdgeModelValidator : AbstractValidator<EdgeModel>
{
    public EdgeModelValidator(CatalogDbContext dbContext)
    {
        RuleFor(x => x.Name)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Укажите название кромки")
            .MaximumLength(250).WithMessage("Название кромки не должно быть длиннее 250 символов");
        RuleFor(x => x.Article)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Укажите артикул кромки")
            .MaximumLength(100).WithMessage("Артикул не должен быть длиннее 100 символов");
        RuleFor(x => x.Price)
            .Cascade(CascadeMode.Stop)
            .GreaterThanOrEqualTo(0).WithMessage("Стоимость не может быть отрицательной")
            .PrecisionScale(18, 2, true).WithMessage("Стоимость должна содержать не более 16 цифр до точки и двух после точки");
        RuleFor(x => x.ManufacturerId)
            .Cascade(CascadeMode.Stop)
            .GreaterThan(0).WithMessage("Выберите производителя кромки")
            .MustAsync(async (id, ct) => await dbContext.EdgeManufacturers.AsNoTracking().AnyAsync(x => x.Id == id, ct))
            .WithMessage("Производитель кромки не найден");
        RuleFor(x => x.ThicknessId)
            .Cascade(CascadeMode.Stop)
            .GreaterThan(0).WithMessage("Выберите толщину кромки")
            .MustAsync(async (id, ct) => await dbContext.EdgeThicknesses.AsNoTracking().AnyAsync(x => x.Id == id, ct))
            .WithMessage("Толщина кромки не найдена");
        RuleFor(x => x.SaleUnitId)
            .Cascade(CascadeMode.Stop)
            .GreaterThan(0).WithMessage("Выберите единицу продажи")
            .MustAsync(async (id, ct) => await dbContext.EdgeSaleUnits.AsNoTracking().AnyAsync(x => x.Id == id, ct))
            .WithMessage("Единица продажи кромки не найдена");
        RuleFor(x => x.StockQuantity)
            .Cascade(CascadeMode.Stop)
            .NotNull().When(x => x.IsStockItem, ApplyConditionTo.CurrentValidator).WithMessage("Укажите остаток складской позиции")
            .GreaterThanOrEqualTo(0).When(x => x.IsStockItem, ApplyConditionTo.CurrentValidator).WithMessage("Остаток не может быть отрицательным")
            .PrecisionScale(18, 3, true).When(x => x.IsStockItem, ApplyConditionTo.CurrentValidator).WithMessage("Остаток должен содержать не более 15 цифр до точки и трёх после точки")
            .Null().When(x => !x.IsStockItem, ApplyConditionTo.CurrentValidator).WithMessage("Для позиции под заказ остаток должен быть null");
    }
}