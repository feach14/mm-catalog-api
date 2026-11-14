namespace Catalog.Api.Features.Edges.Sizes.Dto;

[Description("Размер кромок")]
public sealed record EdgeSizeDto(
    [property: Description("Id")] int Id,
    [property: Description("Название")] string Name,
    [property: Description("Толщина в миллиметрах")] decimal Thickness,
    [property: Description("Ширина в миллиметрах")] decimal Width,
    [property: Description("Порядок сортировки")] int OrderByCol,
    [property: Description("Количество кромок")] int Count,
    [property: Description("Количество складских позиций")] int StockCount,
    [property: Description("Количество нескладских позиций")] int OnOrderCount);