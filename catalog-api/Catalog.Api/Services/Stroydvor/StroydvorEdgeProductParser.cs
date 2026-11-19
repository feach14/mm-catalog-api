using System.Globalization;
using System.Text.RegularExpressions;
using Catalog.Database.Enums;

namespace Catalog.Api.Services.Stroydvor;

public sealed record StroydvorEdgeProductData(
    decimal Price,
    string? Article,
    bool InStock,
    string Name,
    EdgeSaleUnitTypeEnum SaleUnitType,
    decimal? RollLengthM);

public static partial class StroydvorProductParser
{
    private static readonly Regex EdgeRollFormat = new(
        @"(?<![\d.,])(?<length>\d+(?:[.,]\d+)?)\s*[xх×*]\s*\d+(?:[.,]\d+)?\s*[/xх×*]\s*\d+(?:[.,]\d+)?(?![\d.,])",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(1));

    private static readonly Regex EdgeRollLength = new(
        @"(?<![\d.,])(?<length>\d+(?:[.,]\d+)?)\s*м(?![\p{L}])",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(1));

    public static StroydvorEdgeProductData ParseEdge(string html, int productId)
    {
        if (html.Length > 3_000_000)
            throw InvalidProduct("Страница Стройдвора превышает допустимый размер.",
                $"Размер страницы: {html.Length} символов, допустимо не более 3000000.");

        var root = ReadHtml(html);
        var main = First(root, node => node.HasClass("pdp-main"))
                   ?? throw new InvalidDataException("Не найдена основная карточка товара Стройдвора.");
        var name = Text(First(main, node => node.Tag == "h1" && !node.IsRelated));
        if (name is null || !name.Split((char[]?)null, 2, StringSplitOptions.RemoveEmptyEntries)[0]
                .Equals("Кромка", StringComparison.OrdinalIgnoreCase))
            throw InvalidProduct("Карточка Стройдвора не относится к кромке.",
                $"Название карточки Стройдвора: «{name ?? "не указано"}», ожидалось первое слово «Кромка».");

        var priceMeta = First(main, node => node.Tag == "meta" && node.HasItemProp("price") && !node.IsRelated);
        var cart = First(main, node => node.Attribute("product_id") == productId.ToString(CultureInfo.InvariantCulture)
                                      && node.Attribute("price") is not null && !node.IsRelated);
        var metaPrice = priceMeta?.Attribute("content");
        var cartPrice = cart?.Attribute("price");
        var price = ParseDecimal(metaPrice) ?? ParseDecimal(cartPrice);
        if (price is null)
            throw InvalidProduct("На карточке Стройдвора не найдена цена кромки.",
                $"Цена Стройдвора: meta price «{metaPrice ?? "не указана"}», цена корзины «{cartPrice ?? "не указана"}». Ожидалось неотрицательное число.");

        var rawUnit = Text(First(main, node => node.HasClass("pdp-price__unit") && !node.IsRelated));
        var unit = rawUnit?.TrimStart('/').Trim() ?? Property(main, "Единица");
        var saleUnitType = EdgePriceUnit(unit);
        if (saleUnitType is null)
            throw InvalidProduct("Цена кромки Стройдвора указана не за метр или бухту; синхронизация отклонена.",
                $"Единица цены Стройдвора: «{unit ?? "не указана"}», ожидались метр или бухта.");

        var currency = First(main, node => node.Tag == "meta" && node.HasItemProp("priceCurrency") && !node.IsRelated)?.Attribute("content");
        if (currency is not null && !currency.Equals("RUB", StringComparison.OrdinalIgnoreCase))
            throw InvalidProduct("Цена Стройдвора указана не в рублях.",
                $"Валюта Стройдвора: «{currency}», ожидалась «RUB».");

        var article = Text(First(main, node => node.HasClass("pdp-sku") && !node.IsRelated));
        if (article?.StartsWith("Артикул", StringComparison.OrdinalIgnoreCase) == true)
            article = article[(article.IndexOf(':') + 1)..].Trim();
        if (string.IsNullOrWhiteSpace(article)) article = null;
        if (article?.Length > 100)
            throw InvalidProduct("Артикул Стройдвора превышает допустимую длину.",
                $"Длина артикула Стройдвора: {article.Length} символов, допустимо не более 100.");

        // Supplier dimensions are inconsistent; only the roll length needed to convert a roll price is read.
        var rollLength = saleUnitType == EdgeSaleUnitTypeEnum.Roll
            ? ParseDecimal(Property(main, "Длина бухты (м)"))
              ?? ParseDecimal(Property(main, "Длина рулона (м)"))
              ?? ParseDecimal(Property(main, "Длина (м)"))
              ?? ParseDecimal(EdgeRollLength.Match(name).Groups["length"].Value)
              ?? ParseDecimal(EdgeRollFormat.Match(name).Groups["length"].Value)
            : null;

        return new StroydvorEdgeProductData(price.Value, article, InStock(main), name,
            saleUnitType.Value, rollLength);
    }

    private static EdgeSaleUnitTypeEnum? EdgePriceUnit(string? value)
    {
        var unit = value is null ? null : Spaces.Replace(value, "").Replace(".", "", StringComparison.Ordinal).ToLowerInvariant();
        return unit switch
        {
            "м" or "пм" or "погм" or "метр" or "погонныйметр" => EdgeSaleUnitTypeEnum.Meter,
            "бухта" or "бухт" or "бух" or "рулон" or "рул" => EdgeSaleUnitTypeEnum.Roll,
            _ => null
        };
    }
}