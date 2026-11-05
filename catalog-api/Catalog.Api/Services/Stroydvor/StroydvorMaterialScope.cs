namespace Catalog.Api.Services.Stroydvor;

public static class StroydvorMaterialScope
{
    public static bool IsEligible(
        int manufacturerId,
        string materialTypeName,
        int height,
        int width)
    {
        if (GetMaterialType(materialTypeName) is null)
            return false;

        return manufacturerId switch
        {
            3 => HasSize(height, width, 2750, 1830),
            5 => HasSize(height, width, 2750, 1830) || HasSize(height, width, 2800, 2070),
            2 or 7 => HasSize(height, width, 2800, 2070),
            _ => false
        };
    }

    public static StroydvorMaterialTypeEnum? GetMaterialType(string? materialTypeName) =>
        materialTypeName?.Trim().ToUpperInvariant() switch
        {
            "ЛДСП" or "LDSP" => StroydvorMaterialTypeEnum.LDSP,
            "ЛМДФ" or "LMDF" => StroydvorMaterialTypeEnum.LMDF,
            _ => null
        };

    public static string GetMaterialTypeName(StroydvorMaterialTypeEnum materialType) => materialType switch
    {
        StroydvorMaterialTypeEnum.LDSP => "ЛДСП",
        StroydvorMaterialTypeEnum.LMDF => "ЛМДФ",
        _ => throw new ArgumentOutOfRangeException(nameof(materialType))
    };

    private static bool HasSize(int height, int width, int first, int second) =>
        height == first && width == second || height == second && width == first;
}