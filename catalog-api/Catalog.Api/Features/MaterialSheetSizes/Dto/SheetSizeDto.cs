namespace Catalog.Api.Features.MaterialSheetSizes.Dto;

public sealed record SheetSizeDto(
    [property: Description("Id размера материала")] int Id,
    [property: Description("Название размера материала")] string Name,
    [property: Description("Высота материала")] int Height,
    [property: Description("Ширина материала")] int Width,
    [property: Description("Показывать размер материала в фильтрах")] bool ShowInFilters,
    [property: Description("Порядковый номер записи (для сортировки)")] int OrderByCol);