using System.Globalization;
using System.Text.RegularExpressions;
using Catalog.Database.Entities;
using Catalog.Database.Search;

namespace Catalog.Api.Features.Materials.Helpers;

internal static class MaterialSearchQuery
{
    public static IQueryable<Material> Apply(IQueryable<Material> materials, string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
            return materials;

        var tokens = MaterialSearchText.Normalize(search).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        var appliedTokens = new HashSet<string>(StringComparer.Ordinal);
        for (var index = 0; index < tokens.Length; index++)
        {
            var token = tokens[index];
            var hasUnit = token.Length > 2 && IsMillimetres(token[^2..]);
            var numberToken = hasUnit ? token[..^2] : token;
            if (decimal.TryParse(numberToken, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var number))
            {
                if (!hasUnit && index + 1 < tokens.Length && IsMillimetres(tokens[index + 1]))
                {
                    hasUnit = true;
                    index++;
                }

                var canonicalNumber = number.ToString("0.############################", CultureInfo.InvariantCulture);
                if (hasUnit)
                {
                    if (appliedTokens.Add($"mm:{canonicalNumber}"))
                        materials = materials.Where(x => x.MaterialThickness.Value == number
                            || x.MaterialSheetSize.Height == number || x.MaterialSheetSize.Width == number);
                    continue;
                }

                if (!appliedTokens.Add($"number:{numberToken}"))
                    continue;

                var numberPattern = Regex.Escape(canonicalNumber);
                if (!string.Equals(numberToken, canonicalNumber, StringComparison.Ordinal))
                    numberPattern = $"({Regex.Escape(numberToken)}|{numberPattern})";
                // Числа в характеристиках ищутся целиком, а артикул сохраняет поиск по любой части.
                // Npgsql выполняет Regex.IsMatch в PostgreSQL с границами POSIX.
                var pattern = $"(^|[^[:alnum:]_.]){numberPattern}($|[^[:alnum:]_.])";
                var articlePattern = $"%{EscapeLikePattern(numberToken)}%";
                materials = materials.Where(x => Regex.IsMatch(x.SearchText, pattern)
                    || EF.Functions.ILike(x.Article, articlePattern, "\\"));
                continue;
            }

            if (!appliedTokens.Add($"text:{token}"))
                continue;

            var textPattern = $"%{EscapeLikePattern(IsMillimetres(token) ? "мм" : token)}%";
            materials = materials.Where(x => EF.Functions.ILike(x.SearchText, textPattern, "\\"));
        }

        return materials;
    }

    private static bool IsMillimetres(string token) => token is "мм" or "mm";

    private static string EscapeLikePattern(string value) => value
        .Replace("\\", "\\\\", StringComparison.Ordinal)
        .Replace("%", "\\%", StringComparison.Ordinal)
        .Replace("_", "\\_", StringComparison.Ordinal);
}