using Catalog.Database;
using Core.CQRS;

namespace Catalog.Api.Features.Edges.ForCalculate;

[Description("Параметры стоимости кромки")]
public sealed record GetEdgePriceForCalculateQuery(
    [property: Description("Id материала")] int MaterialId,
    [property: Description("Толщина кромки, мм")] decimal Thickness)
    : IQuery<GetEdgePriceForCalculateQueryResult>;

public sealed class GetEdgePriceForCalculateQueryValidator : AbstractValidator<GetEdgePriceForCalculateQuery>
{
    public GetEdgePriceForCalculateQueryValidator(CatalogDbContext dbContext)
    {
        RuleFor(x => x.MaterialId)
            .Cascade(CascadeMode.Stop)
            .GreaterThan(0).WithMessage("Id материала должен быть больше нуля")
            .MustAsync(async (id, ct) => await dbContext.Materials.AsNoTracking().AnyAsync(x => x.Id == id, ct))
            .WithMessage("Материал не найден");
        RuleFor(x => x.Thickness)
            .GreaterThan(0).WithMessage("Толщина кромки должна быть больше нуля");
    }
}

[Description("Стоимость кромки")]
public sealed record GetEdgePriceForCalculateQueryResult(
    [property: Description("Стоимость за погонный метр, руб.")] decimal Price,
    [property: Description("Артикул")] string Article);

public sealed class GetEdgePriceForCalculateQueryHandler(CatalogDbContext dbContext)
    : IQueryHandler<GetEdgePriceForCalculateQuery, GetEdgePriceForCalculateQueryResult>
{
    public async Task<GetEdgePriceForCalculateQueryResult> Handle(GetEdgePriceForCalculateQuery query, CancellationToken ct)
    {
        var prices = await dbContext.MaterialEdgeLinks.AsNoTracking()
            .Where(link => link.MaterialId == query.MaterialId
                           && link.Edge.Thickness.Value == query.Thickness
                           && link.Markings.Any(marking => marking.Marking.Name == CalculateMarkingName))
            .Select(link => new GetEdgePriceForCalculateQueryResult(link.Edge.Price, link.Edge.Article))
            .Take(2)
            .ToArrayAsync(ct);

        if (prices.Length == 0)
            throw new BadHttpRequestException($"Для материала с id={query.MaterialId} не найдена кромка толщиной {query.Thickness} мм с маркировкой «{CalculateMarkingName}»");
        if (prices.Length > 1)
            throw new BadHttpRequestException($"Для материала с id={query.MaterialId} найдено несколько кромок толщиной {query.Thickness} мм с маркировкой «{CalculateMarkingName}». Оставьте одну подходящую связь");

        return prices[0];
    }

    private const string CalculateMarkingName = "Век ЛК";
}