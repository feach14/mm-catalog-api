// ReSharper disable NotAccessedPositionalProperty.Global

using Catalog.Api.Features.Materials.ForAdmin.Dto;
using Catalog.Database;
using Catalog.Database.Entities;
using Core.CQRS;

namespace Catalog.Api.Features.Materials.Public;

public sealed record GetMaterialFiltersQuery(
    [property: Description("Id категорий. Для выбора нескольких значений повторите параметр"), FromQuery] int[]? CategoryId = null,
    [property: Description("Id производителей. Для выбора нескольких значений повторите параметр"), FromQuery] int[]? ManufacturerId = null,
    [property: Description("Толщина в миллиметрах. Для выбора нескольких значений повторите параметр"), FromQuery] double[]? Depth = null,
    [property: Description("Id толщин материалов. Для выбора нескольких значений повторите параметр thicknessId. При совместном выборе с depth применяется пересечение"), FromQuery] int[]? ThicknessId = null,
    [property: Description("Id форматов листа. Для выбора нескольких значений повторите параметр"), FromQuery] int[]? SheetSizeId = null,
    [property: Description("Калькулятор. Для выбора нескольких значений повторите параметр"), FromQuery] MaterialCalculatorEnum[]? Calculator = null,
    [property: Description("Поиск по названию или артикулу, до 250 символов"), FromQuery] string? Search = null,
    [property: Description("Наличие материала: true — количество больше нуля, false — количество меньше единицы"), FromQuery] bool? InStock = null
) : IQuery<GetMaterialFiltersQueryResult>;

public sealed class GetMaterialFiltersQueryValidator : AbstractValidator<GetMaterialFiltersQuery>
{
    public GetMaterialFiltersQueryValidator(CatalogDbContext dbContext)
    {
        RuleFor(x => x.Search)
            .Must(search => search is null || search.Trim().Length <= 250)
            .WithMessage("Поисковая строка не должна превышать 250 символов");

        RuleFor(x => x.CategoryId)
            .Cascade(CascadeMode.Stop)
            .Must(values => values is null || values.Length <= 100).WithMessage("Нельзя передать более 100 значений категорий")
            .Must(values => values is null || values.All(value => value > 0)).WithMessage("Id категорий должны быть положительными числами")
            .MustAsync(async (ids, ct) => await AllIdsExist(ids, dbContext.MaterialCategories.Select(x => x.Id), ct))
            .WithMessage("Одна или несколько категорий не найдены");
        RuleFor(x => x.ManufacturerId)
            .Cascade(CascadeMode.Stop)
            .Must(values => values is null || values.Length <= 100).WithMessage("Нельзя передать более 100 значений производителей")
            .Must(values => values is null || values.All(value => value > 0)).WithMessage("Id производителей должны быть положительными числами")
            .MustAsync(async (ids, ct) => await AllIdsExist(ids, dbContext.MaterialManufacturers.Select(x => x.Id), ct))
            .WithMessage("Один или несколько производителей не найдены");
        RuleFor(x => x.SheetSizeId)
            .Cascade(CascadeMode.Stop)
            .Must(values => values is null || values.Length <= 100).WithMessage("Нельзя передать более 100 значений форматов листа")
            .Must(values => values is null || values.All(value => value > 0)).WithMessage("Id форматов листа должны быть положительными числами")
            .MustAsync(async (ids, ct) => await AllIdsExist(ids, dbContext.MaterialSheetSizes.Select(x => x.Id), ct))
            .WithMessage("Один или несколько форматов листа не найдены");

        RuleFor(x => x.ThicknessId)
            .Cascade(CascadeMode.Stop)
            .Must(ids => ids is null || ids.Length <= 100).WithMessage("Нельзя передать более 100 значений толщины")
            .Must(ids => ids is null || ids.All(id => id > 0)).WithMessage("Id толщин должны быть положительными числами")
            .MustAsync(async (ids, ct) => await AllIdsExist(ids, dbContext.MaterialThicknesses.AsNoTracking().Select(x => x.Id), ct))
            .WithMessage("Одна или несколько толщин не найдены");

        RuleFor(x => x.Depth)
            .Must(values => values is null || values.Length <= 100).WithMessage("Нельзя передать более 100 значений толщины")
            .Must(values => values is null || values.All(value => double.IsFinite(value) && value > 0)).WithMessage("Толщина должна быть положительным конечным числом");
        RuleFor(x => x.Calculator)
            .Must(values => values is null || values.Length <= 100).WithMessage("Нельзя передать более 100 значений калькулятора");
        RuleForEach(x => x.Calculator!).IsInEnum().When(x => x.Calculator is not null).WithMessage("Указан неизвестный калькулятор");

    }

    private static async Task<bool> AllIdsExist(int[]? ids, IQueryable<int> existingIds, CancellationToken ct)
    {
        var distinctIds = ids?.Distinct().ToArray() ?? [];
        return distinctIds.Length == 0
               || await existingIds.CountAsync(id => distinctIds.Contains(id), ct) == distinctIds.Length;
    }
}

public sealed record GetMaterialFiltersQueryResult(
    [property: Description("Категории материалов")] MaterialCategoryFilterDto[] Categories,
    [property: Description("Производители материалов")] MaterialManufacturerFilterDto[] Manufacturers,
    [property: Description("Толщины материалов")] MaterialDepthFilterDto[] Depths,
    [property: Description("Толщины материалов из справочника")] MaterialThicknessFilterDto[] Thicknesses,
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

public sealed record MaterialThicknessFilterDto(
    [property: Description("Id толщины материала")] int Id,
    [property: Description("Название толщины материала")] string Name,
    [property: Description("Толщина в миллиметрах")] double Value,
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
        var selectedCategoryIds = (query.CategoryId ?? []).Distinct().ToArray();
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
        var selectedManufacturerIds = (query.ManufacturerId ?? []).Distinct().ToArray();
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
        var thicknesses = await dbContext.MaterialThicknesses.AsNoTracking()
            .Where(thickness => (query.ThicknessId != null && query.ThicknessId.Contains(thickness.Id))
                || depthsBase.Any(material => material.MaterialThicknessId == thickness.Id))
            .OrderBy(thickness => thickness.Value)
            .Select(thickness => new MaterialThicknessFilterDto(
                thickness.Id,
                thickness.Name,
                thickness.Value,
                depthsBase.Count(material => material.MaterialThicknessId == thickness.Id)))
            .ToArrayAsync(ct);
        var depths = await depthsBase
            .Select(material => material.MaterialThickness.Value)
            .Union(query.Depth ?? Array.Empty<double>())
            .OrderBy(value => value)
            .Select(value => new MaterialDepthFilterDto(
                value,
                depthsBase.Count(material => material.MaterialThickness.Value == value)))
            .ToArrayAsync(ct);

        var sheetSizesBase = BaseQuery(query, ExcludedFilterEnum.SheetSize);
        var selectedSheetSizeIds = (query.SheetSizeId ?? []).Distinct().ToArray();
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
            thicknesses,
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
        materials = materials.FilterByCalculators((query.Calculator ?? []).Distinct().ToArray());
        var search = query.Search?.Trim();
        if (!string.IsNullOrEmpty(search))
        {
            var pattern = $"%{EscapeLikePattern(search)}%";
            materials = materials.Where(x =>
                EF.Functions.ILike(x.Name, pattern, "\\")
                || EF.Functions.ILike(x.Article, pattern, "\\"));
        }

        if (excluded != ExcludedFilterEnum.Category && query.CategoryId is { Length: > 0 })
        {
            var ids = query.CategoryId.Distinct().ToArray();
            materials = materials.Where(x => ids.Contains(x.CategoryId));
        }
        if (excluded != ExcludedFilterEnum.Manufacturer && query.ManufacturerId is { Length: > 0 })
        {
            var ids = query.ManufacturerId.Distinct().ToArray();
            materials = materials.Where(x => ids.Contains(x.MaterialManufacturerId));
        }
        if (excluded != ExcludedFilterEnum.Depth && query.ThicknessId is { Length: > 0 })
            materials = materials.Where(x => query.ThicknessId.Contains(x.MaterialThicknessId));

        if (excluded != ExcludedFilterEnum.Depth && query.Depth is { Length: > 0 })
        {
            var values = query.Depth.Distinct().ToArray();
            materials = materials.Where(x => values.Contains(x.MaterialThickness.Value));
        }
        if (excluded != ExcludedFilterEnum.SheetSize && query.SheetSizeId is { Length: > 0 })
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