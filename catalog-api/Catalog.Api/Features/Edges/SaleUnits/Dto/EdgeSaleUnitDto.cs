using Catalog.Database.Enums;

namespace Catalog.Api.Features.Edges.SaleUnits.Dto;

[Description("Единица продажи кромок")]
public sealed record EdgeSaleUnitDto(
    [property: Description("Id")] int Id,
    [property: Description("Название")] string Name,
    [property: Description("Тип: Meter — погонный метр, Roll — бухта"), JsonConverter(typeof(StrictStringEnumConverter))] EdgeSaleUnitTypeEnum Type,
    [property: Description("Длина бухты или кратность отреза в метрах")] decimal Length,
    [property: Description("Количество кромок")] int Count,
    [property: Description("Количество складских позиций")] int StockCount,
    [property: Description("Количество нескладских позиций")] int OnOrderCount);