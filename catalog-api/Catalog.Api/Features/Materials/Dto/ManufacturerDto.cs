namespace Catalog.Api.Features.Materials.Dto;

public sealed record ManufacturerDto(
    [property: Description("Id производителя")] int Id,
    [property: Description("Название производителя")] string Name);
