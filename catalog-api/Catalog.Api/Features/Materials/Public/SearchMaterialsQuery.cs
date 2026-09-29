using Catalog.Api.Features.Materials.ForAdmin.Dto;
using Catalog.Database;
using Catalog.Database.Entities;
using Catalog.Database.Enums;
using Core.CQRS;

namespace Catalog.Api.Features.Materials.Public;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PublicMaterialSortEnum
{
    CatalogOrder = 1,
    NameAsc = 2,
    NameDesc = 3
}

public sealed record SearchMaterialsQuery : IQuery<SearchMaterialsQueryResult>
{
    [Description("Номер страницы, начиная с 1")]
    public int Page { get; init; } = 1;

    [Description("Размер страницы от 1 до 96")]
    public int PageSize { get; init; } = 24;

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

    [Description("Порядок сортировки")]
    public PublicMaterialSortEnum Sort { get; init; } = PublicMaterialSortEnum.CatalogOrder;
}

public sealed class SearchMaterialsQueryValidator : AbstractValidator<SearchMaterialsQuery>
{
    public SearchMaterialsQueryValidator(CatalogDbContext dbContext)
    {
        RuleFor(x => x.Page)
            .Cascade(CascadeMode.Stop)
            .GreaterThanOrEqualTo(1).WithMessage("Номер страницы должен быть не меньше 1")
            .Must((query, page) => query.PageSize < 1 || (long)(page - 1) * query.PageSize <= int.MaxValue)
            .WithMessage("Смещение страницы превышает допустимое значение");
        RuleFor(x => x.PageSize).InclusiveBetween(1, 96).WithMessage("Размер страницы должен быть от 1 до 96");
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
        RuleFor(x => x.Sort).IsInEnum().WithMessage("Указан неизвестный порядок сортировки");
    }

    private static async Task<bool> AllIdsExist(int[] ids, IQueryable<int> existingIds, CancellationToken ct)
    {
        var distinctIds = ids.Distinct().ToArray();
        return distinctIds.Length == 0
               || await existingIds.CountAsync(id => distinctIds.Contains(id), ct) == distinctIds.Length;
    }
}

public sealed record SearchMaterialsQueryResult(
    [property: Description("Материалы текущей страницы")] PublicMaterialListItemDto[] Items,
    [property: Description("Общее количество материалов после фильтрации")] int TotalCount,
    [property: Description("Номер страницы")] int Page,
    [property: Description("Размер страницы")] int PageSize);

public sealed record PublicMaterialListItemDto(
    [property: Description("Id материала")] int Id,
    [property: Description("Название материала")] string Name,
    [property: Description("Артикул материала")] string Article,
    [property: Description("Категория")] PublicMaterialCategoryDto Category,
    [property: Description("Производитель")] PublicMaterialManufacturerDto Manufacturer,
    [property: Description("Толщина материала в миллиметрах")] double Depth,
    [property: Description("Формат листа")] PublicMaterialSheetSizeDto SheetSize,
    [property: Description("Материал есть в наличии")] bool InStock,
    [property: Description("Изображения материала")] PublicMaterialImagesDto Images);

public sealed record PublicMaterialImagesDto(
    [property: Description("Id оригинального изображения")] Guid? Original,
    [property: Description("Id квадратной миниатюры 240 на 240 пикселей")] Guid? Thumbnail240,
    [property: Description("Id квадратной миниатюры 480 на 480 пикселей")] Guid? Thumbnail480);

public sealed record PublicMaterialCategoryDto(
    [property: Description("Id категории")] int Id,
    [property: Description("Название категории")] string Name);

public sealed record PublicMaterialManufacturerDto(
    [property: Description("Id производителя")] int Id,
    [property: Description("Название производителя")] string Name);

public sealed record PublicMaterialSheetSizeDto(
    [property: Description("Id формата листа")] int Id,
    [property: Description("Название формата листа")] string Name,
    [property: Description("Высота листа в миллиметрах")] int Height,
    [property: Description("Ширина листа в миллиметрах")] int Width);

public sealed class SearchMaterialsQueryHandler(CatalogDbContext dbContext)
    : IQueryHandler<SearchMaterialsQuery, SearchMaterialsQueryResult>
{
    public async Task<SearchMaterialsQueryResult> Handle(SearchMaterialsQuery query, CancellationToken ct)
    {
        var materials = ApplyFilters(dbContext.Materials.AsNoTracking(), query);
        var totalCount = await materials.CountAsync(ct);
        var ordered = query.Sort switch
        {
            PublicMaterialSortEnum.NameAsc => materials.OrderBy(x => x.Name).ThenBy(x => x.Id),
            PublicMaterialSortEnum.NameDesc => materials.OrderByDescending(x => x.Name).ThenBy(x => x.Id),
            _ => materials.OrderBy(x => x.Category.OrderByCol).ThenBy(x => x.OrderByCol).ThenBy(x => x.Id)
        };

        var offset = checked((query.Page - 1) * query.PageSize);
        var items = await ordered
            .Skip(offset)
            .Take(query.PageSize)
            .Select(x => new PublicMaterialListItemDto(
                x.Id,
                x.Name,
                x.Article,
                new PublicMaterialCategoryDto(x.CategoryId, x.Category.Name),
                new PublicMaterialManufacturerDto(x.MaterialManufacturer.Id, x.MaterialManufacturer.Name),
                x.Depth,
                new PublicMaterialSheetSizeDto(x.MaterialSheetSize.Id, x.MaterialSheetSize.Name, x.MaterialSheetSize.Height, x.MaterialSheetSize.Width),
                x.Count > 0,
                new PublicMaterialImagesDto(
                    x.Images.Where(image => image.ImageType == MaterialImageTypeEnum.Original).OrderBy(image => image.Id).Select(image => (Guid?)image.Guid).FirstOrDefault(),
                    x.Images.Where(image => image.ImageType == MaterialImageTypeEnum.Thumbnail240).OrderBy(image => image.Id).Select(image => (Guid?)image.Guid).FirstOrDefault(),
                    x.Images.Where(image => image.ImageType == MaterialImageTypeEnum.Thumbnail480).OrderBy(image => image.Id).Select(image => (Guid?)image.Guid).FirstOrDefault())))
            .ToArrayAsync(ct);

        return new SearchMaterialsQueryResult(items, totalCount, query.Page, query.PageSize);
    }

    private static IQueryable<Material> ApplyFilters(IQueryable<Material> materials, SearchMaterialsQuery query)
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

        if (query.CategoryId.Length > 0)
        {
            var ids = query.CategoryId.Distinct().ToArray();
            materials = materials.Where(x => ids.Contains(x.CategoryId));
        }
        if (query.ManufacturerId.Length > 0)
        {
            var ids = query.ManufacturerId.Distinct().ToArray();
            materials = materials.Where(x => ids.Contains(x.MaterialManufacturerId));
        }
        if (query.Depth.Length > 0)
        {
            var values = query.Depth.Distinct().ToArray();
            materials = materials.Where(x => values.Contains(x.Depth));
        }
        if (query.SheetSizeId.Length > 0)
        {
            var ids = query.SheetSizeId.Distinct().ToArray();
            materials = materials.Where(x => ids.Contains(x.MaterialSheetSizeId));
        }
        if (query.InStock.HasValue)
            materials = query.InStock.Value ? materials.Where(x => x.Count > 0) : materials.Where(x => x.Count < 1);

        return materials;
    }

    private static string EscapeLikePattern(string value) => value
        .Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("%", "\\%", StringComparison.Ordinal)
        .Replace("_", "\\_", StringComparison.Ordinal);
}