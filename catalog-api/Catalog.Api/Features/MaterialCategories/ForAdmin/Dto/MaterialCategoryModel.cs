namespace Catalog.Api.Features.MaterialCategories.ForAdmin.Dto;

public sealed record MaterialCategoryModel
{
    [Description("Название категории")]
    public required string Name { get; init; }

    [Description("Ссылка на внешний источник")]
    public required string ExternalLink { get; init; }

    [Description("Признак: скрыть категорию на сайте")]
    public bool HideOnSite { get; init; }

    [Description("Скрыть в ЛК")]
    public bool HideInLk { get; init; }
}

public class MaterialCategoryModelValidator : AbstractValidator<MaterialCategoryModel>
{
    public MaterialCategoryModelValidator()
    {
        RuleFor(m => m.Name)
            .NotEmpty().WithMessage("Не указано название категории")
            .MaximumLength(200).WithMessage("Название категории не должно быть длиннее 200 символов");

        RuleFor(m => m.ExternalLink)
            .NotEmpty().WithMessage("Нет ссылки на источник. Для категорий данное поле обязательно для заполнения.")
            .MaximumLength(250).WithMessage("Ссылка должна содержать не более 250 символов.");
    }
}