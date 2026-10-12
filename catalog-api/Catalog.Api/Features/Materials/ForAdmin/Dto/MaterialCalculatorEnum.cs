namespace Catalog.Api.Features.Materials.ForAdmin.Dto;

[JsonConverter(typeof(StrictStringEnumConverter))]
public enum MaterialCalculatorEnum
{
    Cutting = 1,
    PvhFacades = 2,
    EnamelFacades = 3
}