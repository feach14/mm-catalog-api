using System.Globalization;
using Catalog.Database.Entities;

namespace Catalog.Api.Features.Materials.Helpers;

internal static class MaterialNameHelper
{
    public static string GetFullName(Material material)
    {
        var culture = CultureInfo.GetCultureInfo("ru-RU");
        return $"{material.MaterialType.Name.Trim()} {material.Name.Trim()} {material.MaterialThickness.Value.ToString("0.###", culture)} мм {material.MaterialSheetSize.Height}×{material.MaterialSheetSize.Width} {material.MaterialManufacturer.Name.Trim()}";
    }
}