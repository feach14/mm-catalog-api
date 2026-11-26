using Catalog.Database;

namespace Catalog.Api.Features.Edges.Suppliers.Dto;

[Description("Параметры поставщика кромок")]
public sealed record EdgeSupplierModel(
    [property: Description("Название"), JsonRequired] string Name,
    [property: Description("Включать поставщика в полное название кромки"), JsonRequired] bool IncludeInEdgeName);

public sealed class EdgeSupplierModelValidator : AbstractValidator<EdgeSupplierModel>
{
    public EdgeSupplierModelValidator(CatalogDbContext dbContext, IHttpContextAccessor httpContextAccessor)
    {
        RuleFor(x => x.Name)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Укажите название поставщика кромок")
            .MaximumLength(100).WithMessage("Название не должно быть длиннее 100 символов")
            .MustAsync(async (name, ct) =>
            {
                string? routeId = httpContextAccessor.HttpContext?.Request.RouteValues["id"]?.ToString();
                _ = int.TryParse(routeId, out int id);
                string normalizedName = name.Trim().ToLowerInvariant();
                return !await dbContext.EdgeSuppliers.AsNoTracking().AnyAsync(
                    x => x.Id != id && EF.Property<string>(x, "NormalizedName") == normalizedName, ct);
            }).WithMessage("Поставщик с таким названием уже существует.");
    }
}