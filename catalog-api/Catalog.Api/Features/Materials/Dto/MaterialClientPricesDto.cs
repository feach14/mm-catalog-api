namespace Catalog.Api.Features.Materials.Dto;

public sealed record MaterialClientPricesDto(
    [property: Description("Цена за лист для частного клиента")] decimal PrivateClientPrice,
    [property: Description("Цена за лист для оптового клиента")] decimal WholesaleClientPrice,
    [property: Description("Цена за лист для индивидуального клиента")] decimal IndividualClientPrice);