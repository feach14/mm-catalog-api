using Catalog.Api.Features.Edges.Dto;
using Core.CQRS;
using Core.Exceptions;
using Core.HttpClients;

namespace Catalog.Api.Features.Edges;

public sealed record GetEdgeClientPricePercentagesQuery : IQuery<EdgeClientPricePercentages?>;

public sealed record EdgeClientPricePercentages(
    [property: Description("Наценки по толщине кромки"), JsonRequired] EdgeClientPricePercentageItem[] Items)
{
    public EdgeClientPricesDto? GetPrices(decimal thickness, decimal price, decimal supplierPrice)
    {
        var percentages = Items.SingleOrDefault(item => item.Thickness == thickness);
        if (percentages is null) return null;
        var basePrice = supplierPrice > 0 ? supplierPrice : price;
        return new EdgeClientPricesDto(
            basePrice * (1m + percentages.PrivateClientPercentage / 100m),
            basePrice * (1m + percentages.WholesaleClientPercentage / 100m),
            basePrice * (1m + percentages.IndividualClientPercentage / 100m),
            percentages.PrivateClientPercentage, percentages.WholesaleClientPercentage, percentages.IndividualClientPercentage);
    }
}

public sealed record EdgeClientPricePercentageItem(
    [property: Description("Толщина кромки, мм"), JsonRequired] decimal Thickness,
    [property: Description("Наценка для частного клиента, %"), JsonRequired] int PrivateClientPercentage,
    [property: Description("Наценка для оптового клиента, %"), JsonRequired] int WholesaleClientPercentage,
    [property: Description("Наценка для индивидуального клиента, %"), JsonRequired] int IndividualClientPercentage);

public sealed class GetEdgeClientPricePercentagesQueryHandler(
    IInternalHttpClient httpClient,
    IOptions<AppSettingsConfig> options,
    ILogger<GetEdgeClientPricePercentagesQueryHandler> logger)
    : IQueryHandler<GetEdgeClientPricePercentagesQuery, EdgeClientPricePercentages?>
{
    public async Task<EdgeClientPricePercentages?> Handle(GetEdgeClientPricePercentagesQuery query, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, options.Value.CalculateApi.EdgePricePercentagesPath);
        // При временном отказе Calculate каталог остаётся доступен без неподтверждённых клиентских цен.
        try
        {
            return await httpClient.SendAsync<EdgeClientPricePercentages>(nameof(AppSettingsConfig.CalculateApi), request, ct, requireAuthentication: false);
        }
        catch (InternalHttpRequestException error) when (!ct.IsCancellationRequested
            && (error.StatusCode is StatusCodes.Status503ServiceUnavailable or StatusCodes.Status504GatewayTimeout
                || error.ResponseStatusCode is >= HttpStatusCode.InternalServerError))
        {
            logger.LogWarning(error, "Не удалось получить наценки Calculate API; клиентские цены кромок недоступны.");
            return null;
        }
    }
}