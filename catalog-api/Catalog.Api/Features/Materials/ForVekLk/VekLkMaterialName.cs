using System.Globalization;

namespace Catalog.Api.Features.Materials.ForVekLk;

internal static class VekLkMaterialName
{
    public static string Format(string materialTypeName, string name, decimal thickness, int height, int width, string manufacturerName)
    {
        var culture = CultureInfo.GetCultureInfo("ru-RU");
        return $"{materialTypeName.Trim()} {name.Trim()} {thickness.ToString("0.###", culture)} мм {height}×{width} {manufacturerName.Trim()}";
    }
}