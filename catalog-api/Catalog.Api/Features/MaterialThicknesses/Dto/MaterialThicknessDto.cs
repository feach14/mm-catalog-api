namespace Catalog.Api.Features.MaterialThicknesses.Dto;

public sealed record MaterialThicknessDto(
    [property: Description("Id толщины материала")] int Id,
    [property: Description("Название толщины материала")] string Name,
    [property: Description("Толщина в миллиметрах")] double Value);