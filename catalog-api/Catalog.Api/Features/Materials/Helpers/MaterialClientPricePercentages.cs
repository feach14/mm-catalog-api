using Catalog.Api.Features.Materials.Dto;
using Catalog.Database.AppConstants;
using Core.BaseEnums;

namespace Catalog.Api.Features.Materials.Helpers;

public sealed record MaterialClientPricePercentages(
    int PrivateClientPercentage,
    int WholesaleClientPercentage,
    int IndividualClientPercentage)
{
    public static MaterialClientPricePercentages From(IReadOnlyDictionary<string, int> percentages)
    {
        if (!percentages.TryGetValue($"{PricePercentageConstants.MaterialKeyPrefix}_{PriceTypeEnum.CUSTOM}", out var privatePercentage)
            || !percentages.TryGetValue($"{PricePercentageConstants.MaterialKeyPrefix}_{PriceTypeEnum.MEBEL}", out var wholesalePercentage)
            || !percentages.TryGetValue($"{PricePercentageConstants.MaterialKeyPrefix}_{PriceTypeEnum.KEY}", out var individualPercentage))
            throw new InvalidOperationException("Не заданы наценки на материал. Расчёт невозможен.");

        return new MaterialClientPricePercentages(privatePercentage, wholesalePercentage, individualPercentage);
    }

    public MaterialClientPricesDto GetPrices(decimal price, decimal supplierPrice)
    {
        var basePrice = supplierPrice > 0 ? supplierPrice : price;
        return new MaterialClientPricesDto(
            basePrice * (1m + PrivateClientPercentage / 100m),
            basePrice * (1m + WholesaleClientPercentage / 100m),
            basePrice * (1m + IndividualClientPercentage / 100m),
            PrivateClientPercentage, WholesaleClientPercentage, IndividualClientPercentage);
    }
}