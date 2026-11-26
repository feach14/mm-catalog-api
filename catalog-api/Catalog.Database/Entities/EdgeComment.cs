namespace Catalog.Database.Entities;

public sealed record EdgeComment
{
    public int Id { get; private set; }
    public required int EdgeId { get; init; }
    public Edge Edge { get; private set; } = null!;
    public required string Text { get; init; }
    public bool IsForClient { get; init; }
}