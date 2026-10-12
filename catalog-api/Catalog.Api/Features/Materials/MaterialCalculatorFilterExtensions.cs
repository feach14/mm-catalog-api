using Catalog.Api.Features.Materials.ForAdmin.Dto;
using Catalog.Database.Entities;

namespace Catalog.Api.Features.Materials;

internal static class MaterialCalculatorFilterExtensions
{
    public static IQueryable<Material> FilterByCalculators(
        this IQueryable<Material> query,
        MaterialCalculatorEnum[]? calculators)
    {
        if (calculators is not { Length: > 0 })
            return query;

        var includeCutting = calculators.Contains(MaterialCalculatorEnum.Cutting);
        var includePvhFacades = calculators.Contains(MaterialCalculatorEnum.PvhFacades);
        var includeEnamelFacades = calculators.Contains(MaterialCalculatorEnum.EnamelFacades);

        return query.Where(x =>
            (includeCutting && x.ApplicableToCutting)
            || (includePvhFacades && x.ApplicableToPvhFacades)
            || (includeEnamelFacades && x.ApplicableToEnamelFacades));
    }
}