using Catalog.Database.Enums;

namespace Catalog.Api.Services.History;

public interface ICatalogHistoryWriter
{
    void Add(CatalogHistoryActionTypeEnum actionType, CatalogHistoryEntityTypeEnum entityType, int entityId, string message);
}