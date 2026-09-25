namespace Catalog.Api.Features.MaterialCategories.ForAdmin.Dto;

public class MaterialCategoryModelValidator : AbstractValidator<MaterialCategoryModel>
{
    public MaterialCategoryModelValidator()
    {
        RuleFor(m => m.Name).NotEmpty().WithMessage("Не указано название категории");
        RuleFor(m => m.ExternalLink).NotEmpty().WithMessage("Нет ссылки на источник. Для коллекций данное поле обязательно для заполнения.");
    }
}

public record MaterialCategoryModel
{
    [Description("Название категории")]
    public required string Name { get; init; }
    
    [Description("Ссылка на внешний источник")]
    public required string ExternalLink { get; init; }
}