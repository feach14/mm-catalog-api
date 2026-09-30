using Catalog.Database;

namespace Catalog.Api.Features.MaterialThicknesses.ForAdmin.Dto;

public sealed record MaterialThicknessModel
{
    [Description("Название толщины материала")]
    public required string Name { get; init; }

    [Description("Толщина в миллиметрах, не более трёх знаков после десятичной точки")]
    public required decimal Value { get; init; }
}

public sealed class MaterialThicknessModelValidator : AbstractValidator<MaterialThicknessModel>
{
    public MaterialThicknessModelValidator(CatalogDbContext dbContext, IHttpContextAccessor httpContextAccessor)
    {
        RuleFor(x => x.Name)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Не указано название толщины материала")
            .MaximumLength(100).WithMessage("Название толщины материала не должно быть длиннее 100 символов");

        RuleFor(x => x.Value)
            .Cascade(CascadeMode.Stop)
            .Must(value => value > 0).WithMessage("Толщина должна быть положительным числом")
            .PrecisionScale(18, 3, true).WithMessage("Толщина должна содержать не более 15 цифр до точки и трёх после точки");
    }
}