namespace Catalog.Api.Features.Exports.Dto;

public sealed record ExcelExportColumnsResult(
    [property: Description("Доступные поля выгрузки")] IReadOnlyList<ExcelExportFieldDto> Items);

public sealed record ExcelExportFieldDto(
    [property: Description("Ключ поля")] string Key,
    [property: Description("Заголовок колонки")] string Title,
    [property: Description("Выбрано по умолчанию")] bool DefaultSelected);