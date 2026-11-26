namespace Catalog.Api.Features.Edges.Suppliers.Dto;

[Description("Поставщик кромок")]
public sealed record EdgeSupplierDto(
    [property: Description("Id")] int Id,
    [property: Description("Название")] string Name,
    [property: Description("Включать поставщика в полное название кромки")] bool IncludeInEdgeName,
    [property: Description("Порядок сортировки")] int OrderByCol,
    [property: Description("Количество кромок")] int Count,
    [property: Description("Количество складских позиций")] int StockCount,
    [property: Description("Количество нескладских позиций")] int OnOrderCount);