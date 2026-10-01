namespace Catalog.Api.Features.Materials.ForAdmin.Dto;

public sealed record MaterialImagesDto
{
    [Description("GUID оригинального изображения")]
    public Guid? Original { get; init; }

    [Description("Формат и размер оригинального изображения")]
    public MaterialImageMetadataDto? OriginalMetadata { get; init; }

    [Description("GUID квадратной миниатюры 240 на 240 пикселей")]
    public Guid? Thumbnail240 { get; init; }

    [Description("Формат и размер квадратной миниатюры 240 на 240 пикселей")]
    public MaterialImageMetadataDto? Thumbnail240Metadata { get; init; }

    [Description("GUID квадратной миниатюры 480 на 480 пикселей")]
    public Guid? Thumbnail480 { get; init; }

    [Description("Формат и размер квадратной миниатюры 480 на 480 пикселей")]
    public MaterialImageMetadataDto? Thumbnail480Metadata { get; init; }
}

public sealed record MaterialImageMetadataDto
{
    [Description("MIME-тип изображения")]
    public required string ContentType { get; init; }

    [Description("Размер изображения в байтах")]
    public required int Size { get; init; }
}