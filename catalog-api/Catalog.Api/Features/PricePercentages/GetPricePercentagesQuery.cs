using Catalog.Database;
using Catalog.Database.Enums;
using Core.BaseEnums;
using Core.CQRS;

namespace Catalog.Api.Features.PricePercentages;

public sealed record GetPricePercentagesQuery(
    [property: Description("К чему применяется")] PricePercentageTargetEnum? AppliesTo = null) : IQuery<GetPricePercentagesQueryResult>;

public sealed class GetPricePercentagesQueryValidator : AbstractValidator<GetPricePercentagesQuery>
{
    public GetPricePercentagesQueryValidator()
    {
        RuleFor(query => query.AppliesTo)
            .IsInEnum().WithMessage("Неизвестное назначение процента.");
    }
}

public sealed record GetPricePercentagesQueryResult(
    [property: Description("Проценты для цен")] PricePercentageDto[] Items);

public sealed record PricePercentageDto(
    [property: Description("Id")] int Id,
    [property: Description("Код")] string Key,
    [property: Description("Название")] string Name,
    [property: Description("Значение в процентах")] int Value,
    [property: Description("К чему применяется"), JsonConverter(typeof(StrictStringEnumConverter))] PricePercentageTargetEnum AppliesTo,
    [property: Description("Тип клиента"), JsonConverter(typeof(StrictStringEnumConverter))] PriceTypeEnum PriceType,
    [property: Description("Описание")] string Description);

public sealed class GetPricePercentagesQueryHandler(CatalogDbContext dbContext)
    : IQueryHandler<GetPricePercentagesQuery, GetPricePercentagesQueryResult>
{
    public async Task<GetPricePercentagesQueryResult> Handle(GetPricePercentagesQuery query, CancellationToken ct) =>
        new(await dbContext.PricePercentages.AsNoTracking()
            .Where(percentage => !query.AppliesTo.HasValue || percentage.AppliesTo == query.AppliesTo)
            .OrderBy(percentage => percentage.Id)
            .Select(percentage => new PricePercentageDto(percentage.Id, percentage.Key, percentage.Name,
                percentage.Value, percentage.AppliesTo, percentage.PriceType, percentage.Description))
            .ToArrayAsync(ct));
}