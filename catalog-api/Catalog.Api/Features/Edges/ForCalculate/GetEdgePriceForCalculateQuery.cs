using Catalog.Database;
using Core.CQRS;

namespace Catalog.Api.Features.Edges.ForCalculate;

[Description("Параметры стоимости кромки")]
public sealed record GetEdgePriceForCalculateQuery(
    [property: Description("Id материала")] int MaterialId,
    [property: Description("Толщина кромки, мм")] decimal Thickness,
    [property: Description("Подбор с маркировкой «Век ЛК»")] bool ForVek = false)
    : IQuery<GetEdgePriceForCalculateQueryResult>;

public sealed class GetEdgePriceForCalculateQueryValidator : AbstractValidator<GetEdgePriceForCalculateQuery>
{
    public GetEdgePriceForCalculateQueryValidator(CatalogDbContext dbContext)
    {
        RuleFor(x => x.MaterialId)
            .Cascade(CascadeMode.Stop)
            .GreaterThan(0).WithMessage("Id материала должен быть больше нуля")
            .MustAsync(async (id, ct) => await dbContext.Materials.AsNoTracking().Where(x => x.Id == id).Select(x => x.Id)
                .Concat(dbContext.ArchivedMaterials.AsNoTracking().Where(x => x.Id == id).Select(x => x.Id))
                .AnyAsync(ct))
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
        var matches = await dbContext.MaterialEdgeLinks.AsNoTracking()
            .Where(link => link.OwnerMaterialId == query.MaterialId
                           && link.Edge.Thickness.Value == query.Thickness
                           && (!query.ForVek || link.Markings.Any(marking => marking.Marking.Name == CalculateMarkingName)))
            .Select(link => new { link.Edge.Price, link.Edge.Article })
            .Take(2)
            .ToArrayAsync(ct);

        var markingDescription = query.ForVek ? $" с маркировкой «{CalculateMarkingName}»" : string.Empty;
        if (matches.Length == 0)
            throw new BadHttpRequestException($"Для выбранного материала недоступна кромка толщиной {query.Thickness} мм{markingDescription}. Выберите другую толщину кромки");
        if (matches.Length > 1)
            throw new BadHttpRequestException($"Для материала с id={query.MaterialId} найдено несколько кромок толщиной {query.Thickness} мм{markingDescription}. Оставьте одну подходящую связь");

        return new GetEdgePriceForCalculateQueryResult(matches[0].Price, matches[0].Article);
    }

    private const string CalculateMarkingName = "Век ЛК";
}