using Catalog.Api.Features.Materials.Dto;
using Catalog.Api.Features.Materials.ForAdmin.Dto;
using Catalog.Api.Features.MaterialThicknesses.Dto;
using Catalog.Database;
using Core.CQRS;

namespace Catalog.Api.Features.Materials.ForAdmin;

public sealed record GetAllMaterialsForAdminQuery(
    [property: Description("Калькулятор: Raskroy, PvhFacades или EmalFacades. Для выбора нескольких калькуляторов повторите параметр calculator. Без параметра возвращаются все материалы."), FromQuery] MaterialCalculatorEnum[]? Calculator,
    [property: Description("Id категории(коллекции) материала"), FromQuery] int? CategoryId,
    [property: Description("Наличие материала: true — count > 0, false — count < 1. Без параметра возвращаются все материалы."), FromQuery] bool? InStock,
    [property: Description("Id толщин материалов. Для выбора нескольких значений повторите параметр thicknessId"), FromQuery] int[]? ThicknessId = null
) : IQuery<GetAllMaterialsForAdminQueryResult>;

public sealed class GetAllMaterialsForAdminQueryValidator : AbstractValidator<GetAllMaterialsForAdminQuery>
{
    public GetAllMaterialsForAdminQueryValidator(CatalogDbContext dbContext)
    {
        RuleFor(x => x.ThicknessId)
            .Cascade(CascadeMode.Stop)
            .Must(ids => ids is null || ids.Length <= 100).WithMessage("Нельзя передать более 100 значений толщины")
            .Must(ids => ids is null || ids.All(id => id > 0)).WithMessage("Id толщин должны быть положительными числами")
            .MustAsync(async (ids, ct) => ids is null || ids.Length == 0
                || await dbContext.MaterialThicknesses.AsNoTracking().CountAsync(x => ids.Contains(x.Id), ct) == ids.Distinct().Count())
            .WithMessage("Одна или несколько толщин не найдены");

        RuleForEach(x => x.Calculator!)
            .IsInEnum()
            .When(x => x.Calculator is not null)
            .WithMessage("Указан недопустимый калькулятор");
    }
}

public sealed record GetAllMaterialsForAdminQueryResult(
    [property: Description("Список материалов")] GetMaterialsQueryForAdminItemResult[] Items,
    [property: Description("Справочник толщин материалов")] MaterialThicknessDto[] Thicknesses);

public sealed record GetMaterialsQueryForAdminItemResult(
    [property: Description("Id материала")] int Id,
    [property: Description("Название материала")] string Name,
    [property: Description("Артикул материала")] string Article,
    [property: Description("Размер материала")] MaterialSheetSizeDto SheetSize,
    [property: Description("Производитель")] MaterialManufacturerDto Manufacturer,
    [property: Description("Толщина материала")] MaterialThicknessDto Thickness,
    [property: Description("Категория")] CategoryDto Category,
    [property: Description("Ссылка на внешний источник")] string? ExternalLink,
    [property: Description("Количество")] int Count,
    [property: Description("Порядковый номер записи (для сортировки)")] int OrderByCol,
    [property: Description("Стоимость материала")] decimal Price
);

public class GetAllMaterialsForAdminQueryHandler(CatalogDbContext dbContext) : IQueryHandler<GetAllMaterialsForAdminQuery, GetAllMaterialsForAdminQueryResult>
{
    public async Task<GetAllMaterialsForAdminQueryResult> Handle(GetAllMaterialsForAdminQuery query, CancellationToken ct)
    {
        var materialsQuery = dbContext.Materials.AsNoTracking().FilterByCalculators(query.Calculator);

        if (query.ThicknessId is { Length: > 0 })
            materialsQuery = materialsQuery.Where(x => query.ThicknessId.Contains(x.MaterialThicknessId));

        if (query.CategoryId.HasValue)
            materialsQuery = materialsQuery.Where(x => x.CategoryId == query.CategoryId.Value);

        if (query.InStock.HasValue)
            materialsQuery = query.InStock.Value
                ? materialsQuery.Where(x => x.Count > 0)
                : materialsQuery.Where(x => x.Count < 1);

        var materials = await materialsQuery
            .OrderBy(x => x.Category.OrderByCol)
            .ThenBy(x => x.OrderByCol)
            .Select(x => new GetMaterialsQueryForAdminItemResult(
                x.Id,
                x.Name,
                x.Article,
                new MaterialSheetSizeDto(
                    x.MaterialSheetSize.Id,
                    x.MaterialSheetSize.Name,
                    x.MaterialSheetSize.Height,
                    x.MaterialSheetSize.Width),
                new MaterialManufacturerDto(x.MaterialManufacturer.Id, x.MaterialManufacturer.Name),
                new MaterialThicknessDto(x.MaterialThickness.Id, x.MaterialThickness.Name, x.MaterialThickness.Value),
                new CategoryDto(x.CategoryId, x.Category.Name),
                x.ExternalLink,
                x.Count,
                x.OrderByCol,
                x.Price))
            .ToArrayAsync(ct);

        var thicknesses = await dbContext.MaterialThicknesses.AsNoTracking()
            .OrderBy(x => x.Value)
            .Select(x => new MaterialThicknessDto(x.Id, x.Name, x.Value))
            .ToArrayAsync(ct);
        return new GetAllMaterialsForAdminQueryResult(materials, thicknesses);
    }
}