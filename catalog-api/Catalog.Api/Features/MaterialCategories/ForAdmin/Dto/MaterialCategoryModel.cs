using Catalog.Database;

namespace Catalog.Api.Features.MaterialCategories.ForAdmin.Dto;

public class MaterialCategoryModelValidator : AbstractValidator<MaterialCategoryModel>
{
    public MaterialCategoryModelValidator(CatalogDbContext dbContext, IHttpContextAccessor httpContextAccessor)
    {
        RuleFor(m => m.Name)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Не указано название категории")
            .MustAsync(async (name, ct) =>
            {
                var routeId = httpContextAccessor.HttpContext?.Request.RouteValues["id"]?.ToString();
                var id = int.TryParse(routeId, out var parsedId) ? parsedId : 0;
                return !await dbContext.MaterialCategories.AnyAsync(
                    x => x.Id != id && x.Name == name, ct);
            })
            .WithMessage("Категория материалов с таким названием уже существует");
        RuleFor(m => m.ExternalLink).NotEmpty().WithMessage("Нет ссылки на источник. Для коллекций данное поле обязательно для заполнения.");
    }
}

public record MaterialCategoryModel
{
    [Description("Название категории")]
    public required string Name { get; init; }

    [Description("Ссылка на внешний источник")]
    public required string ExternalLink { get; init; }
}