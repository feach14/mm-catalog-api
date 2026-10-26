using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Catalog.Api.Services.Stroydvor;

public sealed record StroydvorProductData(
    decimal Price,
    string? Article,
    bool InStock,
    string Name,
    decimal? ThicknessMm,
    int? Height,
    int? Width);

public static class StroydvorProductParser
{
    private static readonly Regex Tokens = new(
        @"<!--[\s\S]*?-->|<(script|style)\b([^>]*)>([\s\S]*?)</\1\s*>|</([a-z][\w:-]*)\s*>|<([a-z][\w:-]*)\b([^>]*)>|[^<]+|<",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(2));

    private static readonly Regex Attributes = new(
        "([^\\s=/>]+)(?:\\s*=\\s*(?:\"([^\"]*)\"|'([^']*)'|([^\\s>]+)))?",
        RegexOptions.CultureInvariant,
        TimeSpan.FromSeconds(2));

    private static readonly Regex Spaces = new(@"\s+", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));
    private static readonly Regex Size = new(@"(?<h>\d{3,4})\s*[xх×]\s*(?<w>\d{3,4})", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));
    private static readonly Regex Thickness = new(@"(?<t>\d{1,2}(?:[.,]\d+)?)\s*мм\b", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));

    private static readonly HashSet<string> VoidTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "area", "base", "br", "col", "embed", "hr", "img", "input", "link", "meta", "param", "source", "track", "wbr"
    };

    public static StroydvorProductData Parse(string html, int productId)
    {
        if (html.Length > 3_000_000)
            throw new InvalidDataException("Страница Стройдвора превышает допустимый размер.");

        var root = ReadHtml(html);
        var main = First(root, node => node.HasClass("pdp-main"))
                   ?? throw new InvalidDataException("Не найдена основная карточка товара Стройдвора.");
        var name = Text(First(main, node => node.Tag == "h1" && !node.IsRelated));
        if (string.IsNullOrWhiteSpace(name) || !name.StartsWith("ЛДСП", StringComparison.OrdinalIgnoreCase)
                                            || name.Contains("МДФ", StringComparison.OrdinalIgnoreCase)
                                            || name.Contains("ХДФ", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Карточка Стройдвора не относится к ЛДСП.");

        var priceMeta = First(main, node => node.Tag == "meta" && node.HasItemProp("price") && !node.IsRelated);
        var cart = First(main, node => node.Attribute("product_id") == productId.ToString(CultureInfo.InvariantCulture)
                                      && node.Attribute("price") is not null && !node.IsRelated);
        var price = ParseDecimal(priceMeta?.Attribute("content")) ?? ParseDecimal(cart?.Attribute("price"));
        if (price is null)
            throw new InvalidDataException("На карточке Стройдвора не найдена цена материала.");

        var unit = Text(First(main, node => node.HasClass("pdp-price__unit") && !node.IsRelated))?.TrimStart('/').Trim()
                   ?? Property(main, "Единица");
        if (!IsSheetUnit(unit))
            throw new InvalidDataException("Цена Стройдвора указана не за лист; синхронизация отклонена.");

        var currency = First(main, node => node.Tag == "meta" && node.HasItemProp("priceCurrency") && !node.IsRelated)?.Attribute("content");
        if (currency is not null && !currency.Equals("RUB", StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Цена Стройдвора указана не в рублях.");

        var article = Text(First(main, node => node.HasClass("pdp-sku") && !node.IsRelated));
        if (article?.StartsWith("Артикул", StringComparison.OrdinalIgnoreCase) == true)
            article = article[(article.IndexOf(':') + 1)..].Trim();
        if (string.IsNullOrWhiteSpace(article)) article = null;
        if (article?.Length > 100)
            throw new InvalidDataException("Артикул Стройдвора превышает допустимую длину.");

        var titleSize = ParseSize(name);
        var propertySize = ParseSize(Property(main, "Формат (мм)"));
        if (titleSize is not null && propertySize is not null && !SameSize(titleSize.Value, propertySize.Value))
            throw new InvalidDataException("В карточке Стройдвора расходятся форматы листа в названии и характеристиках.");
        var size = propertySize ?? titleSize;
        var titleThicknessMatch = Thickness.Match(name);
        var titleThickness = titleThicknessMatch.Success ? ParseDecimal(titleThicknessMatch.Groups["t"].Value) : null;
        var propertyThickness = ParseDecimal(Property(main, "Толщина (мм)"));
        if (titleThickness is not null && propertyThickness is not null && titleThickness != propertyThickness)
            throw new InvalidDataException("В карточке Стройдвора расходится толщина в названии и характеристиках.");

        return new StroydvorProductData(price.Value, article, InStock(main), name,
            propertyThickness ?? titleThickness, size?.Height, size?.Width);
    }

    private static bool IsSheetUnit(string? value)
    {
        var unit = value?.Replace(".", "", StringComparison.Ordinal).Trim().ToLowerInvariant();
        return unit is "л" or "лист" or "листы" or "шт" or "плита";
    }

    private static bool InStock(HtmlNode main)
    {
        var stock = First(main, node => node.HasClass("pdp-stock") && !node.IsRelated);
        if (stock?.HasClass("pdp-stock--in") == true) return true;
        if (stock?.HasClass("pdp-stock--out") == true) return false;

        var schema = First(main, node => node.HasItemProp("availability") && !node.IsRelated);
        var availability = schema?.Attribute("href") ?? schema?.Attribute("content");
        if (availability?.EndsWith("/InStock", StringComparison.OrdinalIgnoreCase) == true) return true;
        if (availability?.EndsWith("/OutOfStock", StringComparison.OrdinalIgnoreCase) == true) return false;

        var warehouses = new Dictionary<string, decimal>(StringComparer.Ordinal);
        foreach (var store in Descendants(main).Where(node => node.HasClass("pdp-store") && !node.IsRelated))
        {
            var radio = First(store, node => node.Tag == "input" && node.Attribute("type") == "radio");
            var warehouse = radio?.Attribute("value");
            var quantity = ParseDecimal(radio?.Attribute("count"));
            if (warehouse is null || quantity is null)
                throw new InvalidDataException("Не удалось определить наличие материала в Стройдворе.");
            if (warehouses.TryGetValue(warehouse, out var previous) && previous != quantity)
                throw new InvalidDataException("В карточке Стройдвора расходятся остатки одного склада.");
            warehouses[warehouse] = quantity.Value;
        }
        if (warehouses.Count > 0) return warehouses.Values.Any(quantity => quantity > 0);
        throw new InvalidDataException("На карточке Стройдвора не указано наличие материала.");
    }

    private static string? Property(HtmlNode main, string key)
    {
        foreach (var row in Descendants(main).Where(node => node.HasClass("pdp-props__row") && !node.IsRelated))
        {
            if (Text(First(row, node => node.HasClass("k")))?.TrimEnd(':') == key)
                return Text(First(row, node => node.HasClass("v")));
        }
        return null;
    }

    private static decimal? ParseDecimal(string? value)
    {
        if (value is null) return null;
        var cleaned = Spaces.Replace(WebUtility.HtmlDecode(value), "").Replace(',', '.');
        return decimal.TryParse(cleaned, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var parsed) && parsed >= 0
            ? parsed
            : null;
    }

    private static (int Height, int Width)? ParseSize(string? value)
    {
        if (value is null) return null;
        var match = Size.Match(value);
        return match.Success
            ? (int.Parse(match.Groups["h"].Value, CultureInfo.InvariantCulture), int.Parse(match.Groups["w"].Value, CultureInfo.InvariantCulture))
            : null;
    }

    private static bool SameSize((int Height, int Width) first, (int Height, int Width) second) =>
        first == second || first.Height == second.Width && first.Width == second.Height;

    private static HtmlNode ReadHtml(string html)
    {
        var root = new HtmlNode("", new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase), null);
        var stack = new List<HtmlNode> { root };
        foreach (Match token in Tokens.Matches(html))
        {
            if (token.Value.StartsWith("<!--", StringComparison.Ordinal)) continue;
            if (token.Groups[4].Success)
            {
                var closing = token.Groups[4].Value;
                var index = stack.FindLastIndex(node => node.Tag.Equals(closing, StringComparison.OrdinalIgnoreCase));
                if (index > 0) stack.RemoveRange(index, stack.Count - index);
                continue;
            }
            if (token.Groups[1].Success || token.Groups[5].Success)
            {
                var tag = (token.Groups[1].Success ? token.Groups[1].Value : token.Groups[5].Value).ToLowerInvariant();
                var source = token.Groups[1].Success ? token.Groups[2].Value : token.Groups[6].Value;
                var attributes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (Match attribute in Attributes.Matches(source))
                {
                    var value = attribute.Groups[2].Success ? attribute.Groups[2].Value
                        : attribute.Groups[3].Success ? attribute.Groups[3].Value : attribute.Groups[4].Value;
                    attributes[attribute.Groups[1].Value] = WebUtility.HtmlDecode(value);
                }
                var node = new HtmlNode(tag, attributes, stack[^1]);
                stack[^1].Children.Add(node);
                if (token.Groups[1].Success) node.Children.Add(token.Groups[3].Value);
                else if (!VoidTags.Contains(tag) && !token.Value.EndsWith("/>", StringComparison.Ordinal))
                {
                    if (stack.Count >= 256) throw new InvalidDataException("Страница Стройдвора содержит слишком глубокую разметку.");
                    stack.Add(node);
                }
            }
            else stack[^1].Children.Add(token.Value);
        }
        return root;
    }

    private static HtmlNode? First(HtmlNode root, Func<HtmlNode, bool> predicate) => Descendants(root).FirstOrDefault(predicate);

    private static IEnumerable<HtmlNode> Descendants(HtmlNode root)
    {
        var stack = new Stack<HtmlNode>();
        stack.Push(root);
        while (stack.TryPop(out var node))
        {
            yield return node;
            for (var index = node.Children.Count - 1; index >= 0; index--)
                if (node.Children[index] is HtmlNode child) stack.Push(child);
        }
    }

    private static string? Text(HtmlNode? root)
    {
        if (root is null) return null;
        var buffer = new StringBuilder();
        var stack = new Stack<object>();
        stack.Push(root);
        while (stack.TryPop(out var value))
        {
            if (value is string text) buffer.Append(text).Append(' ');
            else if (value is HtmlNode node && node.Tag is not ("script" or "style"))
                for (var index = node.Children.Count - 1; index >= 0; index--) stack.Push(node.Children[index]);
        }
        var result = Spaces.Replace(WebUtility.HtmlDecode(buffer.ToString()), " ").Trim();
        return result.Length == 0 ? null : result;
    }

    private sealed record HtmlNode(string Tag, Dictionary<string, string> Attributes, HtmlNode? Parent)
    {
        public List<object> Children { get; } = [];
        public string? Attribute(string name) => Attributes.GetValueOrDefault(name);
        public bool HasClass(string name) => (Attribute("class") ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries).Contains(name, StringComparer.Ordinal);
        public bool HasItemProp(string name) => (Attribute("itemprop") ?? "").Split(' ', StringSplitOptions.RemoveEmptyEntries).Contains(name, StringComparer.OrdinalIgnoreCase);

        public bool IsRelated
        {
            get
            {
                for (HtmlNode? node = this; node is not null; node = node.Parent)
                    if (node.HasClass("pdp-related") || node.HasClass("pcard")) return true;
                return false;
            }
        }
    }
}