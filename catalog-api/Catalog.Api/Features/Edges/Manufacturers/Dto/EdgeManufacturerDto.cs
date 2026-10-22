namespace Catalog.Api.Features.Edges.Manufacturers.Dto;

[Description("Производитель кромок")]
public sealed record EdgeManufacturerDto(
    [property: Description("Id")] int Id,
    [property: Description("Название")] string Name,
    [property: Description("Количество кромок")] int Count,
    [property: Description("Количество складских позиций")] int StockCount,
    [property: Description("Количество нескладских позиций")] int OnOrderCount);