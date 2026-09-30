namespace Catalog.Api.Features.MaterialSheetSizes.ForAdmin.Dto;

public sealed record MaterialSheetSizeModel(
    [property: Description("Название размера материала")] string Name,
    [property: Description("Высота материала")] int Height,
    [property: Description("Ширина материала")] int Width,
    [property: Description("Показывать размер материала в фильтрах")] bool ShowInFilters);

public sealed class SheetSizeModelValidator : AbstractValidator<MaterialSheetSizeModel>
{
    public SheetSizeModelValidator()
    {
        RuleFor(x => x.Name)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Не указано название размера материала")
            .MaximumLength(100).WithMessage("Название размера материала не должно быть длиннее 100 символов");
        RuleFor(x => x.Height).GreaterThan(0).WithMessage("Высота материала должна быть больше нуля");
        RuleFor(x => x.Width).GreaterThan(0).WithMessage("Ширина материала должна быть больше нуля");
    }
}