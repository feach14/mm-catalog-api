namespace Catalog.Api.Features.Edges.ForAdmin.Dto;

public sealed record EdgeImagesDto(
    [property: Description("GUID оригинального изображения")] Guid? Original,
    [property: Description("Формат и размер оригинального изображения")] EdgeImageMetadataDto? OriginalMetadata,
    [property: Description("GUID миниатюры")] Guid? Thumbnail240,
    [property: Description("Формат и размер миниатюры")] EdgeImageMetadataDto? Thumbnail240Metadata);

public sealed record EdgeImageMetadataDto(
    [property: Description("MIME-тип изображения")] string ContentType,
    [property: Description("Размер изображения в байтах")] int Size);