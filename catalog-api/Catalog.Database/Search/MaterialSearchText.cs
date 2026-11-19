using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Catalog.Database.Entities;

namespace Catalog.Database.Search;

public static partial class MaterialSearchText
{
    public static string Normalize(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormKC).ToLowerInvariant().Replace('ё', 'е');
        normalized = DecimalComma().Replace(normalized, ".");
        normalized = DimensionSeparator().Replace(normalized, " ");
        return Whitespace().Replace(normalized, " ").Trim();
    }

    internal static string Create(Material material, MaterialType materialType,
        MaterialManufacturer manufacturer, MaterialThickness thickness, MaterialSheetSize sheetSize)
    {
        var value = string.Join(' ', material.Name, material.Article, materialType.Name,
            manufacturer.Name, thickness.Name, sheetSize.Name,
            thickness.Value.ToString("0.###", CultureInfo.InvariantCulture), "мм",
            sheetSize.Height.ToString(CultureInfo.InvariantCulture), sheetSize.Width.ToString(CultureInfo.InvariantCulture));
        return " " + Normalize(value) + " ";
    }

    [GeneratedRegex(@"(?<=\d),(?=\d)", RegexOptions.CultureInvariant)]
    private static partial Regex DecimalComma();

    [GeneratedRegex(@"(?<=\d)\s*[xх×*]\s*(?=\d)", RegexOptions.CultureInvariant)]
    private static partial Regex DimensionSeparator();

    [GeneratedRegex(@"\s+", RegexOptions.CultureInvariant)]
    private static partial Regex Whitespace();
}