using Catalog.Database;
using Catalog.Database.Enums;
using Core.CQRS;

namespace Catalog.Api.Features.History;

public sealed record GetCatalogHistoryQuery(
    [property: FromQuery(Name = "page")] int Page = 1,
    [property: FromQuery(Name = "pageSize")] int PageSize = 50,
    [property: FromQuery(Name = "actionType")] CatalogHistoryActionType? ActionType = null,
    [property: FromQuery(Name = "entityType")] CatalogHistoryEntityType? EntityType = null,
    [property: FromQuery(Name = "userPhone")] string? UserPhone = null,
    [property: FromQuery(Name = "from")] DateTimeOffset? From = null,
    [property: FromQuery(Name = "to")] DateTimeOffset? To = null,
    [property: FromQuery(Name = "search")] string? Search = null) : IQuery<GetCatalogHistoryQueryResult>;

public sealed class GetCatalogHistoryQueryValidator : AbstractValidator<GetCatalogHistoryQuery>
{
    public GetCatalogHistoryQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThan(0);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 200);
        RuleFor(x => x.ActionType).IsInEnum().When(x => x.ActionType.HasValue);
        RuleFor(x => x.EntityType).IsInEnum().When(x => x.EntityType.HasValue);
        RuleFor(x => x.To).GreaterThanOrEqualTo(x => x.From).When(x => x.From.HasValue && x.To.HasValue);
    }
}

public sealed record CatalogHistoryItemDto(
    long Id,
    DateTimeOffset OccurredAt,
    CatalogHistoryActionType ActionType,
    CatalogHistoryEntityType EntityType,
    int EntityId,
    string UserPhone,
    string Message);

public sealed record GetCatalogHistoryQueryResult(
    CatalogHistoryItemDto[] Items,
    int TotalCount,
    int Page,
    int PageSize);

public sealed class GetCatalogHistoryQueryHandler(CatalogDbContext dbContext)
    : IQueryHandler<GetCatalogHistoryQuery, GetCatalogHistoryQueryResult>
{
    public async Task<GetCatalogHistoryQueryResult> Handle(GetCatalogHistoryQuery query, CancellationToken ct)
    {
        var historyQuery = dbContext.ChangeHistory.AsNoTracking();

        if (query.ActionType.HasValue)
            historyQuery = historyQuery.Where(x => x.ActionType == query.ActionType.Value);
        if (query.EntityType.HasValue)
            historyQuery = historyQuery.Where(x => x.EntityType == query.EntityType.Value);
        if (!string.IsNullOrWhiteSpace(query.UserPhone))
            historyQuery = historyQuery.Where(x => x.UserPhone == query.UserPhone.Trim());
        if (query.From.HasValue)
            historyQuery = historyQuery.Where(x => x.OccurredAt >= query.From.Value);
        if (query.To.HasValue)
            historyQuery = historyQuery.Where(x => x.OccurredAt <= query.To.Value);
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var pattern = $"%{query.Search.Trim()}%";
            historyQuery = historyQuery.Where(x => EF.Functions.ILike(x.Message, pattern));
        }

        var totalCount = await historyQuery.CountAsync(ct);
        var items = await historyQuery
            .OrderByDescending(x => x.OccurredAt)
            .ThenByDescending(x => x.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(x => new CatalogHistoryItemDto(
                x.Id,
                x.OccurredAt,
                x.ActionType,
                x.EntityType,
                x.EntityId,
                x.UserPhone,
                x.Message))
            .ToArrayAsync(ct);

        return new GetCatalogHistoryQueryResult(items, totalCount, query.Page, query.PageSize);
    }
}