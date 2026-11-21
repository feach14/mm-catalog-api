namespace Catalog.Api.Services.Stroydvor;

public static class StroydvorMaterialScope
{
    public static bool IsEligible(string materialTypeName) => GetMaterialType(materialTypeName) is not null;

    public static StroydvorMaterialTypeEnum? GetMaterialType(string? materialTypeName) =>
        materialTypeName?.Trim().ToUpperInvariant() switch
        {
            "ЛДСП" or "LDSP" => StroydvorMaterialTypeEnum.LDSP,
            "ЛМДФ" or "LMDF" => StroydvorMaterialTypeEnum.LMDF,
            "ХДФ" or "HDF" => StroydvorMaterialTypeEnum.HDF,
            _ => null
        };

    public static string GetMaterialTypeName(StroydvorMaterialTypeEnum materialType) => materialType switch
    {
        StroydvorMaterialTypeEnum.LDSP => "ЛДСП",
        StroydvorMaterialTypeEnum.LMDF => "ЛМДФ",
        StroydvorMaterialTypeEnum.HDF => "ХДФ",
        _ => throw new ArgumentOutOfRangeException(nameof(materialType))
    };
}