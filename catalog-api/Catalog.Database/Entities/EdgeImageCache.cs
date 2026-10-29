using Catalog.Database.Enums;

namespace Catalog.Database.Entities;

public sealed record EdgeImageCache
{
    public required Guid Guid { get; init; }
    public required string FileName { get; init; }
    public required string ContentType { get; init; }
    public required byte[] Data { get; init; }
    public required EdgeImageTypeEnum ImageType { get; init; }
}