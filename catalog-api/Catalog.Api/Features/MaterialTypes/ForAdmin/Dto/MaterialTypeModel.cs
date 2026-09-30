namespace Catalog.Api.Features.MaterialTypes.ForAdmin.Dto;

public sealed record MaterialTypeModel
{
    [Description("Название типа материала")]
    public required string Name { get; init; }
}

public sealed class MaterialTypeModelValidator : AbstractValidator<MaterialTypeModel>
{
    public MaterialTypeModelValidator()
    {
        RuleFor(x => x.Name)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Не указано название типа материала")
            .MaximumLength(100).WithMessage("Название типа материала не должно быть длиннее 100 символов");
    }
}