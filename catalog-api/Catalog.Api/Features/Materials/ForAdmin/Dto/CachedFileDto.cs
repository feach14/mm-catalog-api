namespace Catalog.Api.Features.Materials.ForAdmin.Dto;

public sealed record CachedFileDto(
    [property: Description("Название файла")] string FileName, 
    [property:JsonIgnore] byte[] Data,
    [property: Description("Guid файла")] Guid FileGuid, 
    [property: Description("Тип файла")] string ContentType);
