namespace Catalog.Api.Features.Manufacturers.Dto;

public sealed record ManufacturerDto(
    [property: Description("Id производителя")] int Id,
    [property: Description("Название производителя")] string Name,
    [property: Description("Порядковый номер записи (для сортировки)")] int OrderByCol,
    [property: Description("Количество материалов в наличии")] int MaterialsAnyCount,
    [property: Description("Количество материалов не в наличии")] int MaterialsNotAnyCount);