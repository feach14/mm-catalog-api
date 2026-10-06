// ReSharper disable NotAccessedPositionalProperty.Global

using Catalog.Api.Features.Materials.Dto;
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

public sealed record SearchMaterialsQuery(
    [property: Description("ID категорий"), FromQuery] int[]? CategoryIds = null,
    [property: Description("ID производителей"), FromQuery] int[]? ManufacturerIds = null,
    [property: Description("ID типов материалов"), FromQuery] int[]? MaterialTypeIds = null,
    [property: Description("Толщины в миллиметрах"), FromQuery] decimal[]? Depths = null,
    [property: Description("ID толщин материалов"), FromQuery] int[]? ThicknessIds = null,
    [property: Description("ID форматов листа"), FromQuery] int[]? SheetSizeIds = null,
    [property: Description("Варианты использования материала"), FromQuery] MaterialCalculatorEnum[]? Calculators = null,
    [property: Description("Номер страницы, начиная с 1"), FromQuery] int Page = 1,
    [property: Description("Размер страницы от 1 до 100"), FromQuery] int PageSize = 25,
    [property: Description("Поиск по названию или артикулу, до 250 символов"), FromQuery] string? Search = null,
    [property: Description("Наличие материала: true — количество больше нуля, false — количество меньше единицы"), FromQuery] bool? InStock = null,
    [property: Description("Порядок сортировки"), FromQuery] PublicMaterialSortEnum Sort = PublicMaterialSortEnum.CatalogOrder
) : IQuery<SearchMaterialsQueryResult>;

public sealed class SearchMaterialsQueryValidator : AbstractValidator<SearchMaterialsQuery>
{
    public SearchMaterialsQueryValidator(CatalogDbContext dbContext)
    {
        RuleFor(x => x.Page)
            .Cascade(CascadeMode.Stop)
            .GreaterThanOrEqualTo(1).WithMessage("Номер страницы должен быть не меньше 1")
            .Must((query, page) => query.PageSize < 1 || (long)(page - 1) * query.PageSize <= int.MaxValue)
            .WithMessage("Смещение страницы превышает допустимое значение");
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100).WithMessage("Размер страницы должен быть от 1 до 100");
        RuleFor(x => x.Search)
            .Must(search => search is null || search.Trim().Length <= 250)
            .WithMessage("Поисковая строка не должна превышать 250 символов");

        RuleFor(x => x.CategoryIds)
            .Cascade(CascadeMode.Stop)
            .Must(values => values is null || values.Length <= 100).WithMessage("Нельзя передать более 100 значений категорий")
            .Must(values => values is null || values.All(value => value > 0)).WithMessage("Id категорий должны быть положительными числами")
            .MustAsync(async (ids, ct) => await AllIdsExist(ids, dbContext.MaterialCategories.Where(x => !x.HideOnSite).Select(x => x.Id), ct))
            .WithMessage("Одна или несколько категорий не найдены");
        RuleFor(x => x.ManufacturerIds)
            .Cascade(CascadeMode.Stop)
            .Must(values => values is null || values.Length <= 100).WithMessage("Нельзя передать более 100 значений производителей")
            .Must(values => values is null || values.All(value => value > 0)).WithMessage("Id производителей должны быть положительными числами")
            .MustAsync(async (ids, ct) => await AllIdsExist(ids, dbContext.MaterialManufacturers.Select(x => x.Id), ct))
            .WithMessage("Один или несколько производителей не найдены");
        RuleFor(x => x.MaterialTypeIds)
            .Cascade(CascadeMode.Stop)
            .Must(values => values is null || values.Length <= 100).WithMessage("Нельзя передать более 100 значений типов материалов")
            .Must(values => values is null || values.All(value => value > 0)).WithMessage("Id типов материалов должны быть положительными числами")
            .MustAsync(async (ids, ct) => await AllIdsExist(ids, dbContext.MaterialTypes.Select(x => x.Id), ct))
            .WithMessage("Один или несколько типов материалов не найдены");
        RuleFor(x => x.SheetSizeIds)
            .Cascade(CascadeMode.Stop)
            .Must(values => values is null || values.Length <= 100).WithMessage("Нельзя передать более 100 значений форматов листа")
            .Must(values => values is null || values.All(value => value > 0)).WithMessage("Id форматов листа должны быть положительными числами")
            .MustAsync(async (ids, ct) => await AllIdsExist(ids, dbContext.MaterialSheetSizes.Select(x => x.Id), ct))
            .WithMessage("Один или несколько форматов листа не найдены");

        RuleFor(x => x.ThicknessIds)
            .Cascade(CascadeMode.Stop)
            .Must(ids => ids is null || ids.Length <= 100).WithMessage("Нельзя передать более 100 значений толщины")
            .Must(ids => ids is null || ids.All(id => id > 0)).WithMessage("Id толщин должны быть положительными числами")
            .MustAsync(async (ids, ct) => await AllIdsExist(ids, dbContext.MaterialThicknesses.AsNoTracking().Select(x => x.Id), ct))
            .WithMessage("Одна или несколько толщин не найдены");

        RuleFor(x => x.Depths)
            .Must(values => values is null || values.Length <= 100).WithMessage("Нельзя передать более 100 значений толщины")
            .Must(values => values is null || values.All(value => value > 0)).WithMessage("Толщина должна быть положительным конечным числом");
        RuleFor(x => x.Calculators)
            .Must(values => values is null || values.Length <= 100).WithMessage("Нельзя передать более 100 вариантов использования материала");
        RuleForEach(x => x.Calculators!).IsInEnum().When(x => x.Calculators is not null).WithMessage("Указан неизвестный вариант использования материала");
        RuleFor(x => x.Sort).IsInEnum().WithMessage("Указан неизвестный порядок сортировки");
    }

    private static async Task<bool> AllIdsExist(int[]? ids, IQueryable<int> existingIds, CancellationToken ct)
    {
        var distinctIds = ids?.Distinct().ToArray() ?? [];
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
    [property: Description("Категория")] PropertyDto Category,
    [property: Description("Производитель")] PropertyDto Manufacturer,
    [property: Description("Тип материала")] PropertyDto MaterialType,
    [property: Description("Толщина материала")] PropertyDto Thickness,
    [property: Description("Формат листа")] PropertyDto SheetSize,
    [property: Description("Изображения материала")] PublicMaterialImagesDto Images,
    [property: Description("Материал есть в наличии")] bool InStock
);

public sealed record PublicMaterialImagesDto(
    [property: Description("Id оригинального изображения")] Guid? Original,
    [property: Description("Id квадратной миниатюры 240 на 240 пикселей")] Guid? Thumbnail240,
    [property: Description("Id квадратной миниатюры 480 на 480 пикселей")] Guid? Thumbnail480);

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
                Category: new PropertyDto(x.CategoryId, x.Category.Name),
                Manufacturer: new PropertyDto(x.MaterialManufacturer.Id, x.MaterialManufacturer.Name),
                MaterialType: new PropertyDto(x.MaterialType.Id, x.MaterialType.Name),
                Thickness: new PropertyDto(x.MaterialThickness.Id, x.MaterialThickness.Name),
                SheetSize: new PropertyDto(x.MaterialSheetSize.Id, x.MaterialSheetSize.Name),
                Images: new PublicMaterialImagesDto(
                    Original: x.Images.Where(image => image.ImageType == MaterialImageTypeEnum.Original).OrderBy(image => image.Id).Select(image => (Guid?)image.Guid).FirstOrDefault(),
                    Thumbnail240: x.Images.Where(image => image.ImageType == MaterialImageTypeEnum.Thumbnail240).OrderBy(image => image.Id).Select(image => (Guid?)image.Guid).FirstOrDefault(),
                    Thumbnail480: x.Images.Where(image => image.ImageType == MaterialImageTypeEnum.Thumbnail480).OrderBy(image => image.Id).Select(image => (Guid?)image.Guid).FirstOrDefault()),
                x.Count > 0))
            .ToArrayAsync(ct);

        return new SearchMaterialsQueryResult(items, totalCount, query.Page, query.PageSize);
    }

    private static IQueryable<Material> ApplyFilters(IQueryable<Material> materials, SearchMaterialsQuery query)
    {
        materials = materials.Where(x => !x.HideOnSite && !x.Category.HideOnSite);
        materials = materials.FilterByCalculators((query.Calculators ?? []).Distinct().ToArray());
        var search = query.Search?.Trim();
        if (!string.IsNullOrEmpty(search))
        {
            var pattern = $"%{EscapeLikePattern(search)}%";
            materials = materials.Where(x =>
                EF.Functions.ILike(x.Name, pattern, "\\")
                || EF.Functions.ILike(x.Article, pattern, "\\"));
        }

        if (query.CategoryIds is { Length: > 0 })
        {
            var ids = query.CategoryIds.Distinct().ToArray();
            materials = materials.Where(x => ids.Contains(x.CategoryId));
        }
        if (query.ManufacturerIds is { Length: > 0 })
        {
            var ids = query.ManufacturerIds.Distinct().ToArray();
            materials = materials.Where(x => ids.Contains(x.MaterialManufacturerId));
        }
        if (query.MaterialTypeIds is { Length: > 0 })
        {
            var ids = query.MaterialTypeIds.Distinct().ToArray();
            materials = materials.Where(x => ids.Contains(x.MaterialTypeId));
        }
        if (query.ThicknessIds is { Length: > 0 })
            materials = materials.Where(x => query.ThicknessIds.Contains(x.MaterialThicknessId));

        if (query.Depths is { Length: > 0 })
        {
            var values = query.Depths.Distinct().ToArray();
            materials = materials.Where(x => values.Contains(x.MaterialThickness.Value));
        }
        if (query.SheetSizeIds is { Length: > 0 })
        {
            var ids = query.SheetSizeIds.Distinct().ToArray();
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