namespace Catalog.Api.Features.Materials.Dto;

public sealed record MaterialManufacturerDto(
    [property: Description("Id производителя")] int Id,
    [property: Description("Название производителя")] string Name);