namespace Catalog.Api.Features.Materials.Dto;

public sealed record SheetSizeDto(
    [property: Description("Id размера материала")] int Id,
    [property: Description("Название размера материала")] string Name,
    [property: Description("Высота материала")] int Height,
    [property: Description("Ширина материала")] int Width);