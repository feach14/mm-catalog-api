// ReSharper disable NotAccessedPositionalProperty.Global

using Catalog.Api.Features.Materials.Dto;
using Catalog.Api.Features.Materials.ForAdmin.Dto;
using Catalog.Api.Features.MaterialThicknesses.Dto;
using Catalog.Database;
using Core.CQRS;

namespace Catalog.Api.Features.Materials.Public;

public sealed record GetAllMaterialsQuery(
    [property: Description("Калькулятор: Raskroy, PvhFacades или EmalFacades. Для выбора нескольких калькуляторов повторите параметр calculator. Без параметра возвращаются все материалы."), FromQuery] MaterialCalculatorEnum[]? Calculator,
    [property: Description("Наличие материала: true — count > 0, false — count < 1. Без параметра возвращаются все материалы."), FromQuery] bool? InStock,
    [property: Description("Id толщин материалов. Для выбора нескольких значений повторите параметр thicknessId"), FromQuery] int[]? ThicknessId = null
) : IQuery<GetAllMaterialsQueryResult>;

public sealed class GetAllMaterialsQueryValidator : AbstractValidator<GetAllMaterialsQuery>
{
    public GetAllMaterialsQueryValidator(CatalogDbContext dbContext)
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

public sealed record GetAllMaterialsQueryResult(
    [property: Description("Список материалов")] GetAllMaterialsQueryItemResult[] Items);

public sealed record GetAllMaterialsQueryItemResult(
    [property: Description("Id материала")] int Id,
    [property: Description("Название материала")] string Name,
    [property: Description("Артикул материала")] string Article,
    [property: Description("Размер материала")] MaterialSheetSizeDto SheetSize,
    [property: Description("Производитель")] MaterialManufacturerDto Manufacturer,
    [property: Description("Толщина материала")] MaterialThicknessDto Thickness,
    [property: Description("Категория")] CategoryDto Category,
    [property: Description("Количество")] int Count,
    [property: Description("Признак: Комментарий к материалу обязателен при оформлении заявки(расчета)")] bool CommentOnMaterialIsRequired,
    [property: Description("Порядковый номер записи (для сортировки)")] int OrderByCol);

public record MaterialCategoryDto(
    [property: Description("Id категории")] int Id,
    [property: Description("Название категории")] string Name,
    [property: Description("Порядковый номер для сортировки")] int OrderByCol);

public class GetAllMaterialsQueryHandler(CatalogDbContext dbContext) : IQueryHandler<GetAllMaterialsQuery, GetAllMaterialsQueryResult>
{
    public async Task<GetAllMaterialsQueryResult> Handle(GetAllMaterialsQuery query, CancellationToken ct)
    {
        var materialsQuery = dbContext.Materials.AsNoTracking().FilterByCalculators(query.Calculator);

        if (query.ThicknessId is { Length: > 0 })
            materialsQuery = materialsQuery.Where(x => query.ThicknessId.Contains(x.MaterialThicknessId));

        if (query.InStock.HasValue)
            materialsQuery = query.InStock.Value
                ? materialsQuery.Where(x => x.Count > 0)
                : materialsQuery.Where(x => x.Count < 1);

        var materials = await materialsQuery
            .OrderBy(x => x.Category.OrderByCol)
            .ThenBy(x => x.OrderByCol)
            .Select(x => new GetAllMaterialsQueryItemResult(
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
                x.Count,
                x.CommentOnMaterialIsRequired,
                x.OrderByCol))
            .ToArrayAsync(ct);

        return new GetAllMaterialsQueryResult(materials);
    }
}