namespace Catalog.Database.Entities;

public sealed record EdgeArticleReplacement
{
    public int Id { get; private set; }
    public required int EdgeId { get; init; }
    public Edge Edge { get; private set; } = null!;
    public required string OldArticle { get; init; }
}