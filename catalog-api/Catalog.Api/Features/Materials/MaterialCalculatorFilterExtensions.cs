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

        var includeRaskroy = calculators.Contains(MaterialCalculatorEnum.Raskroy);
        var includePvhFacades = calculators.Contains(MaterialCalculatorEnum.PvhFacades);
        var includeEmalFacades = calculators.Contains(MaterialCalculatorEnum.EmalFacades);

        return query.Where(x =>
            (includeRaskroy && x.ApplicableToCutting)
            || (includePvhFacades && x.ApplicableToPvhFacades)
            || (includeEmalFacades && x.ApplicableToEnamelFacades));
    }
}