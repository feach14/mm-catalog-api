namespace Catalog.Api.Features.Materials.ForAdmin.Dto;

[JsonConverter(typeof(StrictStringEnumConverter))]
public enum MaterialCalculatorEnum
{
    Raskroy = 1,
    PvhFacades = 2,
    EmalFacades = 3
}