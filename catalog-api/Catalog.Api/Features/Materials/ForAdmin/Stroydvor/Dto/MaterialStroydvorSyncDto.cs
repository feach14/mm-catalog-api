namespace Catalog.Api.Features.Materials.ForAdmin.Stroydvor.Dto;

public sealed record MaterialStroydvorSyncDto(
    [property: Description("Id материала")] int MaterialId,
    [property: Description("Успех синхронизации")] bool Success,
    [property: Description("Ошибка синхронизации")] string? Error,
    [property: Description("Id товара Стройдвора")] int? StroydvorProductId,
    [property: Description("Артикул Стройдвора")] string? StroydvorArticle,
    [property: Description("Цена Стройдвора за лист в рублях")] decimal StroydvorPrice,
    [property: Description("Дата успешной синхронизации со Стройдвором")] DateTimeOffset? StroydvorLastSyncAt,
    [property: Description("В наличии в Стройдворе")] bool StroydvorInStock,
    [property: Description("Техническая ссылка на товар Стройдвора")] string? StroydvorUrl);