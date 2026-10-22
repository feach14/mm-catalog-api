namespace Catalog.Api.Features.Edges.Thicknesses.Dto;

[Description("Толщина кромок")]
public sealed record EdgeThicknessDto(
    [property: Description("Id")] int Id,
    [property: Description("Название")] string Name,
    [property: Description("Толщина в миллиметрах")] decimal Value,
    [property: Description("Количество кромок")] int Count,
    [property: Description("Количество складских позиций")] int StockCount,
    [property: Description("Количество нескладских позиций")] int OnOrderCount);