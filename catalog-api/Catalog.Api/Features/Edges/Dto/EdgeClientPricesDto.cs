namespace Catalog.Api.Features.Edges.Dto;

public sealed record EdgeClientPricesDto(
    [property: Description("Цена для частного клиента: за метр для Meter, за бухту для Roll, руб.")] decimal PrivateClientPrice,
    [property: Description("Цена для оптового клиента: за метр для Meter, за бухту для Roll, руб.")] decimal WholesaleClientPrice,
    [property: Description("Цена для индивидуального клиента: за метр для Meter, за бухту для Roll, руб.")] decimal IndividualClientPrice,
    [property: Description("Наценка для частного клиента, %")] int PrivateClientPercentage,
    [property: Description("Наценка для оптового клиента, %")] int WholesaleClientPercentage,
    [property: Description("Наценка для индивидуального клиента, %")] int IndividualClientPercentage);