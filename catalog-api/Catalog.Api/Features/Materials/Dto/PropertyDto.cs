namespace Catalog.Api.Features.Materials.Dto;

public sealed record PropertyDto(
    [property: Description("Id")] int Id,
    [property: Description("Название")] string Name);