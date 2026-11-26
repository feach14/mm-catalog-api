using Catalog.Api.Features.Edges.Dto;
using Catalog.Database.AppConstants;
using Core.BaseEnums;

namespace Catalog.Api.Features.Edges.Helpers;

public sealed record EdgeClientPricePercentages(EdgeClientPricePercentageItem[] Items)
{
    public static EdgeClientPricePercentages From(IReadOnlyDictionary<string, int> percentages)
    {
        var thicknesses = new[]
        {
            (Thickness: 0.4m, KeyPrefix: PricePercentageConstants.Edge04KeyPrefix),
            (Thickness: 1m, KeyPrefix: PricePercentageConstants.Edge1KeyPrefix),
            (Thickness: 2m, KeyPrefix: PricePercentageConstants.Edge2KeyPrefix)
        };
        var items = thicknesses.Select(thickness =>
        {
            if (!percentages.TryGetValue($"{thickness.KeyPrefix}_{PriceTypeEnum.CUSTOM}", out var privatePercentage)
                || !percentages.TryGetValue($"{thickness.KeyPrefix}_{PriceTypeEnum.MEBEL}", out var wholesalePercentage)
                || !percentages.TryGetValue($"{thickness.KeyPrefix}_{PriceTypeEnum.KEY}", out var individualPercentage))
                throw new InvalidOperationException($"Не заданы наценки на кромку толщиной {thickness.Thickness} мм. Расчёт невозможен.");

            return new EdgeClientPricePercentageItem(thickness.Thickness, privatePercentage, wholesalePercentage, individualPercentage);
        }).ToArray();
        return new EdgeClientPricePercentages(items);
    }

    public EdgeClientPricesDto? GetPrices(decimal thickness, decimal price, decimal supplierPrice)
    {
        // В расчёте кромка 0,8 мм заменяет кромку 1 мм и использует ту же наценку.
        var priceThickness = thickness == 0.8m ? 1m : thickness;
        var percentages = Items.SingleOrDefault(item => item.Thickness == priceThickness);
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
    decimal Thickness,
    int PrivateClientPercentage,
    int WholesaleClientPercentage,
    int IndividualClientPercentage);