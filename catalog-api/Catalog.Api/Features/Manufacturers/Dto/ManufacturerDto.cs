namespace Catalog.Api.Features.Manufacturers.Dto;

public sealed record ManufacturerDto(
    [property: Description("Id производителя")] int Id,
    [property: Description("Название производителя")] string Name,
    [property: Description("Порядковый номер записи (для сортировки)")] int OrderByCol);