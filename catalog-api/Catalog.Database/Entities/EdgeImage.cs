using Catalog.Database.Enums;

namespace Catalog.Database.Entities;

public sealed record EdgeImage
{
    public int Id { get; private set; }
    public required int EdgeId { get; init; }
    public Edge Edge { get; private set; } = null!;
    public required Guid Guid { get; init; }
    public required string Type { get; init; }
    public required byte[] Data { get; init; }
    public required EdgeImageTypeEnum ImageType { get; init; }
}