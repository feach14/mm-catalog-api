using Catalog.Database;

namespace Catalog.Api.Features.Edges.Sizes.Dto;

[Description("Параметры размера кромок")]
public sealed record EdgeSizeModel(
    [property: Description("Название")] string Name,
    [property: Description("Толщина в миллиметрах")] decimal Thickness,
    [property: Description("Ширина в миллиметрах")] decimal Width);

public sealed class EdgeSizeModelValidator : AbstractValidator<EdgeSizeModel>
{
    public EdgeSizeModelValidator(CatalogDbContext dbContext, IHttpContextAccessor httpContextAccessor)
    {
        RuleFor(x => x.Name)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Не указано название размера кромок")
            .MaximumLength(100).WithMessage("Название не должно быть длиннее 100 символов");

        RuleFor(x => x.Thickness)
            .Cascade(CascadeMode.Stop)
            .GreaterThan(0).WithMessage("Толщина должна быть положительным числом")
            .PrecisionScale(18, 3, true).WithMessage("Толщина должна содержать не более 15 цифр до точки и трёх после точки")
            .MustAsync(async (model, thickness, ct) =>
            {
                string? routeId = httpContextAccessor.HttpContext?.Request.RouteValues["id"]?.ToString();
                _ = int.TryParse(routeId, out int id);
                return !await dbContext.EdgeSizes.AsNoTracking().AnyAsync(
                    x => x.Id != id && x.Thickness == thickness && x.Width == model.Width, ct);
            }).WithMessage("Такое сочетание толщины и ширины кромок уже существует.");

        RuleFor(x => x.Width)
            .Cascade(CascadeMode.Stop)
            .GreaterThan(0).WithMessage("Ширина должна быть положительным числом")
            .PrecisionScale(18, 3, true).WithMessage("Ширина должна содержать не более 15 цифр до точки и трёх после точки");
    }
}