using Catalog.Database.Entities;

namespace Catalog.Api.Features.Materials;

internal static class MaterialCalculatorFilterExtensions
{
    public static IQueryable<Material> FilterByCalculators(
        this IQueryable<Material> query,
        MaterialCalculator[]? calculators)
    {
        if (calculators is not { Length: > 0 })
            return query;

        var includeRaskroy = calculators.Contains(MaterialCalculator.Raskroy);
        var includePvhFacades = calculators.Contains(MaterialCalculator.PvhFacades);
        var includeEmalFacades = calculators.Contains(MaterialCalculator.EmalFacades);

        return query.Where(x =>
            (includeRaskroy && x.ApplicableToRaskroys)
            || (includePvhFacades && x.ApplicableToPvhFacades)
            || (includeEmalFacades && x.ApplicableToEmalFacades));
    }
}