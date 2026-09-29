using Catalog.Database;

namespace Catalog.Api.Features.MaterialThicknesses.ForAdmin.Dto;

public record MaterialThicknessModel
{
    [Description("Название толщины материала")]
    public required string Name { get; init; }

    [Description("Толщина в миллиметрах")]
    public required double Value { get; init; }
}

public sealed class MaterialThicknessModelValidator : AbstractValidator<MaterialThicknessModel>
{
    public MaterialThicknessModelValidator(CatalogDbContext dbContext, IHttpContextAccessor httpContextAccessor)
    {
        ClassLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.Name)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Не указано название толщины материала")
            .MaximumLength(100).WithMessage("Название толщины материала не должно быть длиннее 100 символов");
        RuleFor(x => x.Value)
            .Cascade(CascadeMode.Stop)
            .Must(value => double.IsFinite(value) && value > 0).WithMessage("Толщина должна быть положительным конечным числом")
            .MustAsync(async (value, ct) =>
            {
                var routeId = httpContextAccessor.HttpContext?.Request.RouteValues["id"]?.ToString();
                var currentId = int.TryParse(routeId, out var id) ? id : 0;
                return !await dbContext.MaterialThicknesses.AsNoTracking()
                    .AnyAsync(x => x.Value == value && x.Id != currentId, ct);
            }).WithMessage("Такая толщина материала уже существует");
    }
}