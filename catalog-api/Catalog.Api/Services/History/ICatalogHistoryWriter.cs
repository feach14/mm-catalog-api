using Catalog.Database.Enums;

namespace Catalog.Api.Services.History;

public interface ICatalogHistoryWriter
{
    void Add(CatalogHistoryActionType actionType, CatalogHistoryEntityType entityType, int entityId, string message);
}