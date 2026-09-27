namespace Catalog.Api.Features.Materials.Dto;

public sealed record CategoryDto(
    [property: Description("Id категории")] int Id,
    [property: Description("Название категории")] string Name);