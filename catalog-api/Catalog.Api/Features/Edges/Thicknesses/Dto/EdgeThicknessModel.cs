using Catalog.Database;

namespace Catalog.Api.Features.Edges.Thicknesses.Dto;

[Description("Параметры толщины кромок")]
public sealed record EdgeThicknessModel(
    [property: Description("Название")] string Name,
    [property: Description("Толщина в миллиметрах")] decimal Value);

public sealed class EdgeThicknessModelValidator : AbstractValidator<EdgeThicknessModel>
{
    public EdgeThicknessModelValidator(CatalogDbContext dbContext, IHttpContextAccessor httpContextAccessor)
    {
        RuleFor(x => x.Name)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Не указано название толщины кромок")
            .MaximumLength(100).WithMessage("Название не должно быть длиннее 100 символов")
            .MustAsync(async (name, ct) =>
            {
                string? routeId = httpContextAccessor.HttpContext?.Request.RouteValues["id"]?.ToString();
                _ = int.TryParse(routeId, out int id);
                string normalizedName = name.Trim().ToLowerInvariant();
                return !await dbContext.EdgeThicknesses.AsNoTracking().AnyAsync(
                    x => x.Id != id && EF.Property<string>(x, "NormalizedName") == normalizedName, ct);
            }).WithMessage("Запись с таким названием уже существует.");

        RuleFor(x => x.Value)
            .Cascade(CascadeMode.Stop)
            .GreaterThan(0).WithMessage("Толщина должна быть положительным числом")
            .PrecisionScale(18, 3, true).WithMessage("Толщина должна содержать не более 15 цифр до точки и трёх после точки")
            .MustAsync(async (value, ct) =>
            {
                string? routeId = httpContextAccessor.HttpContext?.Request.RouteValues["id"]?.ToString();
                _ = int.TryParse(routeId, out int id);
                return !await dbContext.EdgeThicknesses.AsNoTracking().AnyAsync(
                    x => x.Id != id && x.Value == value, ct);
            }).WithMessage("Такая толщина кромок уже существует.");
    }
}