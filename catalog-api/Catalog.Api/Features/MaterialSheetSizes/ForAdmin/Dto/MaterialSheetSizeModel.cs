using Catalog.Database;

namespace Catalog.Api.Features.MaterialSheetSizes.ForAdmin.Dto;

public sealed record MaterialSheetSizeModel(
    [property: Description("Название размера материала")] string Name,
    [property: Description("Высота материала")] int Height,
    [property: Description("Ширина материала")] int Width,
    [property: Description("Показывать размер материала в фильтрах")] bool ShowInFilters);

public sealed class SheetSizeModelValidator : AbstractValidator<MaterialSheetSizeModel>
{
    public SheetSizeModelValidator(CatalogDbContext dbContext, IHttpContextAccessor httpContextAccessor)
    {
        RuleFor(x => x.Name)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Не указано название размера материала")
            .MaximumLength(100).WithMessage("Название размера материала не должно быть длиннее 100 символов")
            .MustAsync(async (name, ct) =>
            {
                var routeId = httpContextAccessor.HttpContext?.Request.RouteValues["id"]?.ToString();
                _ = int.TryParse(routeId, out var id);
                return !await dbContext.MaterialSheetSizes.AsNoTracking().AnyAsync(
                    x => x.Id != id && x.Name == name, ct);
            }).WithMessage("Размер материала с таким названием уже существует.");
        RuleFor(x => x.Height).GreaterThan(0).WithMessage("Высота материала должна быть больше нуля");
        RuleFor(x => x.Width)
            .Cascade(CascadeMode.Stop)
            .GreaterThan(0).WithMessage("Ширина материала должна быть больше нуля")
            .MustAsync(async (model, width, ct) =>
            {
                var routeId = httpContextAccessor.HttpContext?.Request.RouteValues["id"]?.ToString();
                _ = int.TryParse(routeId, out var id);
                return !await dbContext.MaterialSheetSizes.AsNoTracking().AnyAsync(
                    x => x.Id != id && x.Height == model.Height && x.Width == width, ct);
            }).When(x => x.Height > 0, ApplyConditionTo.CurrentValidator).WithMessage("Такой размер материала уже существует.");
    }
}