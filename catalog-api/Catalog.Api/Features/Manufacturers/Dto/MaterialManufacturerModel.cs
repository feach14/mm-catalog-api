namespace Catalog.Api.Features.Manufacturers.Dto;

public sealed record MaterialManufacturerModel
{
    [Description("Название производителя")]
    public required string Name { get; init; }
}

public sealed class CreateManufacturerCommandValidator : AbstractValidator<MaterialManufacturerModel>
{
    public CreateManufacturerCommandValidator()
    {
        RuleFor(x => x.Name)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Не указано название производителя")
            .MaximumLength(100).WithMessage("Название производителя не должно быть длиннее 100 символов");
    }
}