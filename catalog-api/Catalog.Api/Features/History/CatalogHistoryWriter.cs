using Catalog.Database;
using Catalog.Database.Entities;
using Catalog.Database.Enums;
using Core.Extensions;

namespace Catalog.Api.Features.History;

public sealed class CatalogHistoryWriter(
    CatalogDbContext dbContext,
    IHttpContextAccessor httpContextAccessor,
    TimeProvider timeProvider) : ICatalogHistoryWriter
{
    public void Add(CatalogHistoryActionType actionType, CatalogHistoryEntityType entityType, int entityId, string message)
    {
        if (httpContextAccessor.HttpContext?.User.IsInRole(AppConstants.TesterRoleName) == true)
            return;

        dbContext.ChangeHistory.Add(new CatalogChangeHistory
        {
            OccurredAt = timeProvider.GetUtcNow(),
            ActionType = actionType,
            EntityType = entityType,
            EntityId = entityId,
            UserPhone = httpContextAccessor.GetPhoneNumber(),
            Message = message
        });
    }
}