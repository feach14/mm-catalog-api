using Catalog.Database.Enums;

namespace Catalog.Api.Features.Materials.ForAdmin.Dto;

public class CachedFileDto
{
    [Description("Название файла")]
    public required string FileName { get; init; }

    [Description("Guid файла")]
    public required Guid FileGuid { get; init; }

    [Description("Тип файла")]
    public required string ContentType { get; init; }

    [Description("Назначение изображения")]
    public required MaterialImageTypeEnum ImageType { get; init; }
}
