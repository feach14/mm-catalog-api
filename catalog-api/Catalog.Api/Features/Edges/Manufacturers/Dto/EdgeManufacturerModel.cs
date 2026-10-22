using Catalog.Database;

namespace Catalog.Api.Features.Edges.Manufacturers.Dto;

[Description("Параметры производителя кромок")]
public sealed record EdgeManufacturerModel(
    [property: Description("Название")] string Name);

public sealed class EdgeManufacturerModelValidator : AbstractValidator<EdgeManufacturerModel>
{
    public EdgeManufacturerModelValidator(CatalogDbContext dbContext, IHttpContextAccessor httpContextAccessor)
    {
        RuleFor(x => x.Name)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Не указано название производителя кромок")
            .MaximumLength(100).WithMessage("Название не должно быть длиннее 100 символов")
            .MustAsync(async (name, ct) =>
            {
                string? routeId = httpContextAccessor.HttpContext?.Request.RouteValues["id"]?.ToString();
                _ = int.TryParse(routeId, out int id);
                string normalizedName = name.Trim().ToLowerInvariant();
                return !await dbContext.EdgeManufacturers.AsNoTracking().AnyAsync(
                    x => x.Id != id && EF.Property<string>(x, "NormalizedName") == normalizedName, ct);
            }).WithMessage("Запись с таким названием уже существует.");
    }
}