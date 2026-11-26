namespace Catalog.Api.Features.Edges.Helpers;

internal static class EdgeNameHelper
{
    public static string GetFullName(string edgeName, string sizeName, bool isStockItem,
        string supplierName, bool includeSupplierInEdgeName)
    {
        var supplier = includeSupplierInEdgeName ? supplierName.Trim() : string.Empty;
        var prefix = supplier.Length > 0 ? $"{supplier} " : string.Empty;
        return $"{prefix}Кромка {edgeName.Trim()} {sizeName.Trim()}{(isStockItem ? " (Склад)" : string.Empty)}";
    }
}