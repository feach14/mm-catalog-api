using Catalog.Database;
using Catalog.Database.Enums;
using Core.CQRS;

namespace Catalog.Api.Features.History;

public sealed record GetCatalogHistoryQuery(
    [property: Description("Номер страницы")]
    [property: FromQuery] int Page = 1,
    [property: Description("Количество записей на странице")]
    [property: FromQuery] int PageSize = 50,
    [property: Description("Тип действия: создание, изменение или удаление")]
    [property: FromQuery] CatalogHistoryActionType? ActionType = null,
    [property: Description("Тип сущности каталога")]
    [property: FromQuery] CatalogHistoryEntityType? EntityType = null,
    [property: Description("Номер телефона пользователя, выполнившего изменение")]
    [property: FromQuery] string? UserPhone = null,
    [property: Description("Начало периода изменений")]
    [property: FromQuery] DateTimeOffset? From = null,
    [property: Description("Окончание периода изменений")]
    [property: FromQuery] DateTimeOffset? To = null,
    [property: Description("Строка поиска в описании изменения")]
    [property: FromQuery] string? Search = null) : IQuery<GetCatalogHistoryQueryResult>;

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
    [property: Description("Id записи истории")] long Id,
    [property: Description("Дата и время изменения")] DateTimeOffset OccurredAt,
    [property: Description("Тип действия: создание, изменение или удаление")] CatalogHistoryActionType ActionType,
    [property: Description("Тип изменённой сущности каталога")] CatalogHistoryEntityType EntityType,
    [property: Description("Id изменённой сущности")] int EntityId,
    [property: Description("Номер телефона пользователя, выполнившего изменение")] string UserPhone,
    [property: Description("Описание изменения")] string Message);

public sealed record GetCatalogHistoryQueryResult(
    [property: Description("Записи истории изменений")] CatalogHistoryItemDto[] Items,
    [property: Description("Общее количество записей")] int TotalCount,
    [property: Description("Номер текущей страницы")] int Page,
    [property: Description("Количество записей на странице")] int PageSize);

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