using Catalog.Database.Enums;

namespace Catalog.Database.Entities;

public sealed record CatalogChangeHistory
{
    public long Id { get; private set; }
    public required DateTimeOffset OccurredAt { get; init; }
    public required CatalogHistoryActionType ActionType { get; init; }
    public required CatalogHistoryEntityType EntityType { get; init; }
    public required int EntityId { get; init; }
    public required string UserPhone { get; init; }
    public required string Message { get; init; }
}