#pragma warning disable CS8618 // Properties are populated by EF

namespace Catalog.Database.Entities;

public sealed record ImageCache
{
    public required Guid Guid { get; init; }
    public required string FileName { get; init; }
    public required string Type { get; init; }
    public required byte[] Data { get; init; }
}
