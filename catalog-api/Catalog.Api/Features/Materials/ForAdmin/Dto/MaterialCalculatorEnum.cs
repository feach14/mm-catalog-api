namespace Catalog.Api.Features.Materials.ForAdmin.Dto;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MaterialCalculatorEnum
{
    Raskroy = 1,
    PvhFacades = 2,
    EmalFacades = 3
}