using Catalog.Api.Features.Materials.Dto;
using Core.CQRS;
using Core.Exceptions;
using Core.HttpClients;

namespace Catalog.Api.Features.Materials;

public sealed record GetMaterialClientPricePercentagesQuery : IQuery<MaterialClientPricePercentages?>;

public sealed record MaterialClientPricePercentages(
    [property: Description("Наценка для частного клиента, %"), JsonRequired] int PrivateClientPercentage,
    [property: Description("Наценка для оптового клиента, %"), JsonRequired] int WholesaleClientPercentage,
    [property: Description("Наценка для индивидуального клиента, %"), JsonRequired] int IndividualClientPercentage)
{
    public MaterialClientPricesDto GetPrices(decimal price, decimal supplierPrice)
    {
        var basePrice = supplierPrice > 0 ? supplierPrice : price;
        return new MaterialClientPricesDto(
            basePrice * (1m + PrivateClientPercentage / 100m),
            basePrice * (1m + WholesaleClientPercentage / 100m),
            basePrice * (1m + IndividualClientPercentage / 100m));
    }
}

public sealed class GetMaterialClientPricePercentagesQueryHandler(
    IInternalHttpClient httpClient,
    IOptions<AppSettingsConfig> options,
    ILogger<GetMaterialClientPricePercentagesQueryHandler> logger)
    : IQueryHandler<GetMaterialClientPricePercentagesQuery, MaterialClientPricePercentages?>
{
    public async Task<MaterialClientPricePercentages?> Handle(GetMaterialClientPricePercentagesQuery query, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, options.Value.CalculateApi.MaterialPricePercentagesPath);
        // При временном отказе Calculate каталог остаётся доступен без неподтверждённых клиентских цен.
        try
        {
            return await httpClient.SendAsync<MaterialClientPricePercentages>(nameof(AppSettingsConfig.CalculateApi), request, ct, requireAuthentication: false);
        }
        catch (InternalHttpRequestException error) when (!ct.IsCancellationRequested
            && (error.StatusCode is StatusCodes.Status503ServiceUnavailable or StatusCodes.Status504GatewayTimeout
                || error.ResponseStatusCode is >= HttpStatusCode.InternalServerError))
        {
            logger.LogWarning(error, "Не удалось получить наценки Calculate API; клиентские цены материала недоступны.");
            return null;
        }
    }
}