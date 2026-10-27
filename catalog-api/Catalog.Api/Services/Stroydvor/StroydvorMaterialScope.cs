namespace Catalog.Api.Services.Stroydvor;

public static class StroydvorMaterialScope
{
    public static bool IsEligible(
        int categoryId,
        int manufacturerId,
        int materialTypeId,
        decimal thicknessMm,
        int height,
        int width,
        string name)
    {
        if (materialTypeId != 1 || thicknessMm != 16 || IsAnotherMaterialType(name))
            return false;

        return categoryId switch
        {
            42 => manufacturerId == 3 && HasSize(height, width, 2750, 1830),
            43 => manufacturerId == 5 && HasSize(height, width, 2750, 1830),
            44 => manufacturerId == 5 && HasSize(height, width, 2800, 2070),
            45 => manufacturerId == 2 && HasSize(height, width, 2800, 2070),
            46 => manufacturerId == 7 && HasSize(height, width, 2800, 2070),
            _ => false
        };
    }

    private static bool HasSize(int height, int width, int first, int second) =>
        height == first && width == second || height == second && width == first;

    private static bool IsAnotherMaterialType(string name) =>
        (name.Contains("МДФ", StringComparison.OrdinalIgnoreCase) && !name.Contains("ЛМДФ"))
        || name.Contains("ХДФ", StringComparison.OrdinalIgnoreCase)
        || name.Contains("MDF", StringComparison.OrdinalIgnoreCase)
        || name.Contains("HDF", StringComparison.OrdinalIgnoreCase);
}