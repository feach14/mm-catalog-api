using Catalog.Api.Features.Materials.ForAdmin.Dto;
using Catalog.Database;
using Catalog.Database.Entities;
using Core.CQRS;

namespace Catalog.Api.Features.Materials.Public;

public sealed record GetMaterialFiltersQuery : IQuery<GetMaterialFiltersQueryResult>
{
    [Description("Поиск по названию или артикулу, до 250 символов")]
    public string? Search { get; init; }

    [Description("Id категорий. Для выбора нескольких значений повторите параметр")]
    public int[] CategoryId { get; init; } = [];

    [Description("Id производителей. Для выбора нескольких значений повторите параметр")]
    public int[] ManufacturerId { get; init; } = [];

    [Description("Толщина в миллиметрах. Для выбора нескольких значений повторите параметр")]
    public double[] Depth { get; init; } = [];

    [Description("Id форматов листа. Для выбора нескольких значений повторите параметр")]
    public int[] SheetSizeId { get; init; } = [];

    [Description("Наличие материала: true — количество больше нуля, false — количество меньше единицы")]
    public bool? InStock { get; init; }

    [Description("Калькулятор. Для выбора нескольких значений повторите параметр")]
    public MaterialCalculatorEnum[] Calculator { get; init; } = [];
}

public sealed class GetMaterialFiltersQueryValidator : AbstractValidator<GetMaterialFiltersQuery>
{
    public GetMaterialFiltersQueryValidator(CatalogDbContext dbContext)
    {
        RuleFor(x => x.Search)
            .Must(search => search is null || search.Trim().Length <= 250)
            .WithMessage("Поисковая строка не должна превышать 250 символов");

        RuleFor(x => x.CategoryId)
            .Cascade(CascadeMode.Stop)
            .Must(values => values.Length <= 100).WithMessage("Нельзя передать более 100 значений категорий")
            .Must(values => values.All(value => value > 0)).WithMessage("Id категорий должны быть положительными числами")
            .MustAsync(async (ids, ct) => await AllIdsExist(ids, dbContext.MaterialCategories.Select(x => x.Id), ct))
            .WithMessage("Одна или несколько категорий не найдены");
        RuleFor(x => x.ManufacturerId)
            .Cascade(CascadeMode.Stop)
            .Must(values => values.Length <= 100).WithMessage("Нельзя передать более 100 значений производителей")
            .Must(values => values.All(value => value > 0)).WithMessage("Id производителей должны быть положительными числами")
            .MustAsync(async (ids, ct) => await AllIdsExist(ids, dbContext.MaterialManufacturers.Select(x => x.Id), ct))
            .WithMessage("Один или несколько производителей не найдены");
        RuleFor(x => x.SheetSizeId)
            .Cascade(CascadeMode.Stop)
            .Must(values => values.Length <= 100).WithMessage("Нельзя передать более 100 значений форматов листа")
            .Must(values => values.All(value => value > 0)).WithMessage("Id форматов листа должны быть положительными числами")
            .MustAsync(async (ids, ct) => await AllIdsExist(ids, dbContext.MaterialSheetSizes.Select(x => x.Id), ct))
            .WithMessage("Один или несколько форматов листа не найдены");

        RuleFor(x => x.Depth)
            .Must(values => values.Length <= 100).WithMessage("Нельзя передать более 100 значений толщины")
            .Must(values => values.All(value => double.IsFinite(value) && value > 0)).WithMessage("Толщина должна быть положительным конечным числом");
        RuleFor(x => x.Calculator)
            .Must(values => values.Length <= 100).WithMessage("Нельзя передать более 100 значений калькулятора");
        RuleForEach(x => x.Calculator).IsInEnum().WithMessage("Указан неизвестный калькулятор");

    }

    private static async Task<bool> AllIdsExist(int[] ids, IQueryable<int> existingIds, CancellationToken ct)
    {
        var distinctIds = ids.Distinct().ToArray();
        return distinctIds.Length == 0
               || await existingIds.CountAsync(id => distinctIds.Contains(id), ct) == distinctIds.Length;
    }
}

public sealed record GetMaterialFiltersQueryResult(
    [property: Description("Категории материалов")] MaterialCategoryFilterDto[] Categories,
    [property: Description("Производители материалов")] MaterialManufacturerFilterDto[] Manufacturers,
    [property: Description("Толщины материалов")] MaterialDepthFilterDto[] Depths,
    [property: Description("Форматы листов")] MaterialSheetSizeFilterDto[] SheetSizes,
    [property: Description("Варианты наличия")] MaterialAvailabilityFilterDto[] Availability);

public sealed record MaterialCategoryFilterDto(
    [property: Description("Id категории")] int Id,
    [property: Description("Название категории")] string Name,
    [property: Description("Количество материалов")] int Count);

public sealed record MaterialManufacturerFilterDto(
    [property: Description("Id производителя")] int Id,
    [property: Description("Название производителя")] string Name,
    [property: Description("Количество материалов")] int Count);

public sealed record MaterialDepthFilterDto(
    [property: Description("Толщина в миллиметрах")] double Value,
    [property: Description("Количество материалов")] int Count);

public sealed record MaterialSheetSizeFilterDto(
    [property: Description("Id формата листа")] int Id,
    [property: Description("Название формата листа")] string Name,
    [property: Description("Высота листа в миллиметрах")] int Height,
    [property: Description("Ширина листа в миллиметрах")] int Width,
    [property: Description("Количество материалов")] int Count);

public sealed record MaterialAvailabilityFilterDto(
    [property: Description("Признак наличия материала")] bool Value,
    [property: Description("Количество материалов")] int Count);

public sealed class GetMaterialFiltersQueryHandler(CatalogDbContext dbContext)
    : IQueryHandler<GetMaterialFiltersQuery, GetMaterialFiltersQueryResult>
{
    // Endpoint возвращает связанные фасеты, а не статический набор справочников. Query содержит текущий
    // выбор пользователя, поэтому он валидируется, а счётчик каждой фасеты рассчитывается со всеми
    // активными ограничениями, кроме ограничения самой этой фасеты.
    public async Task<GetMaterialFiltersQueryResult> Handle(GetMaterialFiltersQuery query, CancellationToken ct)
    {
        var categoriesBase = BaseQuery(query, ExcludedFilterEnum.Category);
        var selectedCategoryIds = query.CategoryId.Distinct().ToArray();
        var categories = await dbContext.MaterialCategories
            .AsNoTracking()
            .Where(category =>
                categoriesBase.Any(material => material.CategoryId == category.Id)
                || selectedCategoryIds.Contains(category.Id))
            .OrderBy(category => category.OrderByCol)
            .ThenBy(category => category.Id)
            .Select(category => new MaterialCategoryFilterDto(
                category.Id,
                category.Name,
                categoriesBase.Count(material => material.CategoryId == category.Id)))
            .ToArrayAsync(ct);

        var manufacturersBase = BaseQuery(query, ExcludedFilterEnum.Manufacturer);
        var selectedManufacturerIds = query.ManufacturerId.Distinct().ToArray();
        var manufacturers = await dbContext.MaterialManufacturers
            .AsNoTracking()
            .Where(manufacturer =>
                manufacturersBase.Any(material => material.MaterialManufacturerId == manufacturer.Id)
                || selectedManufacturerIds.Contains(manufacturer.Id))
            .OrderBy(manufacturer => manufacturer.OrderByCol)
            .ThenBy(manufacturer => manufacturer.Id)
            .Select(manufacturer => new MaterialManufacturerFilterDto(
                manufacturer.Id,
                manufacturer.Name,
                manufacturersBase.Count(material => material.MaterialManufacturerId == manufacturer.Id)))
            .ToArrayAsync(ct);

        var depthsBase = BaseQuery(query, ExcludedFilterEnum.Depth);
        var depths = await depthsBase
            .Select(material => material.Depth)
            .Union(query.Depth)
            .OrderBy(value => value)
            .Select(value => new MaterialDepthFilterDto(
                value,
                depthsBase.Count(material => material.Depth == value)))
            .ToArrayAsync(ct);

        var sheetSizesBase = BaseQuery(query, ExcludedFilterEnum.SheetSize);
        var selectedSheetSizeIds = query.SheetSizeId.Distinct().ToArray();
        var sheetSizes = await dbContext.MaterialSheetSizes
            .AsNoTracking()
            .Where(sheetSize =>
                (sheetSize.ShowInFilters
                 && sheetSizesBase.Any(material => material.MaterialSheetSizeId == sheetSize.Id))
                || selectedSheetSizeIds.Contains(sheetSize.Id))
            .OrderBy(sheetSize => sheetSize.OrderByCol)
            .ThenBy(sheetSize => sheetSize.Id)
            .Select(sheetSize => new MaterialSheetSizeFilterDto(
                sheetSize.Id,
                sheetSize.Name,
                sheetSize.Height,
                sheetSize.Width,
                sheetSizesBase.Count(material => material.MaterialSheetSizeId == sheetSize.Id)))
            .ToArrayAsync(ct);

        var availabilityBase = BaseQuery(query, ExcludedFilterEnum.Availability);
        var availableCount = await availabilityBase.CountAsync(material => material.Count > 0, ct);
        var unavailableCount = await availabilityBase.CountAsync(material => material.Count < 1, ct);

        return new GetMaterialFiltersQueryResult(
            categories,
            manufacturers,
            depths,
            sheetSizes,
            [
                new MaterialAvailabilityFilterDto(true, availableCount),
                new MaterialAvailabilityFilterDto(false, unavailableCount)
            ]);
    }

    private IQueryable<Material> BaseQuery(GetMaterialFiltersQuery query, ExcludedFilterEnum excluded) =>
        ApplyFilters(dbContext.Materials.AsNoTracking(), query, excluded);

    private static IQueryable<Material> ApplyFilters(
        IQueryable<Material> materials,
        GetMaterialFiltersQuery query,
        ExcludedFilterEnum excluded)
    {
        materials = materials.FilterByCalculators(query.Calculator.Distinct().ToArray());
        var search = query.Search?.Trim();
        if (!string.IsNullOrEmpty(search))
        {
            var pattern = $"%{EscapeLikePattern(search)}%";
            materials = materials.Where(x =>
                EF.Functions.ILike(x.Name, pattern, "\\")
                || EF.Functions.ILike(x.Article, pattern, "\\"));
        }

        if (excluded != ExcludedFilterEnum.Category && query.CategoryId.Length > 0)
        {
            var ids = query.CategoryId.Distinct().ToArray();
            materials = materials.Where(x => ids.Contains(x.CategoryId));
        }
        if (excluded != ExcludedFilterEnum.Manufacturer && query.ManufacturerId.Length > 0)
        {
            var ids = query.ManufacturerId.Distinct().ToArray();
            materials = materials.Where(x => ids.Contains(x.MaterialManufacturerId));
        }
        if (excluded != ExcludedFilterEnum.Depth && query.Depth.Length > 0)
        {
            var values = query.Depth.Distinct().ToArray();
            materials = materials.Where(x => values.Contains(x.Depth));
        }
        if (excluded != ExcludedFilterEnum.SheetSize && query.SheetSizeId.Length > 0)
        {
            var ids = query.SheetSizeId.Distinct().ToArray();
            materials = materials.Where(x => ids.Contains(x.MaterialSheetSizeId));
        }
        if (excluded != ExcludedFilterEnum.Availability && query.InStock.HasValue)
            materials = query.InStock.Value ? materials.Where(x => x.Count > 0) : materials.Where(x => x.Count < 1);

        return materials;
    }

    private static string EscapeLikePattern(string value) => value
        .Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("%", "\\%", StringComparison.Ordinal)
        .Replace("_", "\\_", StringComparison.Ordinal);

    private enum ExcludedFilterEnum
    {
        Category,
        Manufacturer,
        Depth,
        SheetSize,
        Availability
    }
}