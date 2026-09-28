namespace Catalog.Api.Features.Materials;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MaterialCalculator
{
    Raskroy = 1,
    PvhFacades = 2,
    EmalFacades = 3
}