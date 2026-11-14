namespace Catalog.Api.Features.Edges.Helpers;

internal static class EdgeNameHelper
{
    public static string GetFullName(string edgeName, string sizeName, bool isStockItem)
    {
        return $"Кромка {edgeName.Trim()} {sizeName.Trim()}{(isStockItem ? " (Склад)" : string.Empty)}";
    }
}