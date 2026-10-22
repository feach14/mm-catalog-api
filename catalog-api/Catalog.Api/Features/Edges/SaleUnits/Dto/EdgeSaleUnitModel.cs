using Catalog.Database;
using Catalog.Database.Enums;

namespace Catalog.Api.Features.Edges.SaleUnits.Dto;

[Description("Параметры единицы продажи кромок")]
public sealed record EdgeSaleUnitModel(
    [property: Description("Название")] string Name,
    [property: Description("Тип: Meter — погонный метр, Roll — бухта"), JsonConverter(typeof(StrictStringEnumConverter))] EdgeSaleUnitTypeEnum Type,
    [property: Description("Длина бухты или кратность отреза в метрах")] decimal Length);

public sealed class EdgeSaleUnitModelValidator : AbstractValidator<EdgeSaleUnitModel>
{
    public EdgeSaleUnitModelValidator(CatalogDbContext dbContext, IHttpContextAccessor httpContextAccessor)
    {
        RuleFor(x => x.Name)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Не указано название единицы продажи кромок")
            .MaximumLength(100).WithMessage("Название не должно быть длиннее 100 символов")
            .MustAsync(async (name, ct) =>
            {
                string? routeId = httpContextAccessor.HttpContext?.Request.RouteValues["id"]?.ToString();
                _ = int.TryParse(routeId, out int id);
                string normalizedName = name.Trim().ToLowerInvariant();
                return !await dbContext.EdgeSaleUnits.AsNoTracking().AnyAsync(
                    x => x.Id != id && EF.Property<string>(x, "NormalizedName") == normalizedName, ct);
            }).WithMessage("Запись с таким названием уже существует.");

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage("Выберите тип единицы продажи: Meter или Roll");

        RuleFor(x => x.Length)
            .Cascade(CascadeMode.Stop)
            .GreaterThan(0).WithMessage("Длина бухты или кратность отреза должна быть положительным числом")
            .PrecisionScale(18, 3, true).WithMessage("Длина должна содержать не более 15 цифр до точки и трёх после точки")
            .MustAsync(async (model, length, ct) =>
            {
                string? routeId = httpContextAccessor.HttpContext?.Request.RouteValues["id"]?.ToString();
                _ = int.TryParse(routeId, out int id);
                return !await dbContext.EdgeSaleUnits.AsNoTracking().AnyAsync(
                    x => x.Id != id && x.Type == model.Type && x.Length == length, ct);
            }).When(x => Enum.IsDefined(x.Type), ApplyConditionTo.CurrentValidator).WithMessage("Единица продажи с таким типом и длиной уже существует.");
    }
}