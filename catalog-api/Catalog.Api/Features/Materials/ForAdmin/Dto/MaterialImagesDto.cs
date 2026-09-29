namespace Catalog.Api.Features.Materials.ForAdmin.Dto;

public sealed record MaterialImagesDto
{
    [Description("GUID оригинального изображения")]
    public Guid? Original { get; init; }

    [Description("GUID квадратной миниатюры 240 на 240 пикселей")]
    public Guid? Thumbnail240 { get; init; }

    [Description("GUID квадратной миниатюры 480 на 480 пикселей")]
    public Guid? Thumbnail480 { get; init; }
}