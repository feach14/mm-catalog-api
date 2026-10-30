using System.Globalization;
using Catalog.Database.Entities;

namespace Catalog.Api.Features.Materials.Helpers;

internal static class MaterialNameHelper
{
    public static string GetFullName(Material material)
        => GetFullName(material.Name, material.MaterialType.Name, material.MaterialThickness.Value,
            material.MaterialSheetSize.Height, material.MaterialSheetSize.Width, material.MaterialManufacturer.Name);

    public static string GetFullName(string materialName, string materialTypeName, decimal thickness,
        int height, int width, string manufacturerName)
    {
        var culture = CultureInfo.GetCultureInfo("ru-RU");
        return $"{materialTypeName.Trim()} {materialName.Trim()} {thickness.ToString("0.###", culture)} мм {height}×{width} {manufacturerName.Trim()}";
    }
}