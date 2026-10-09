// ReSharper disable NotAccessedPositionalProperty.Global

using Catalog.Api.Features.Materials.Dto;
using Catalog.Database;
using Core.CQRS;

namespace Catalog.Api.Features.Materials.ForCalculate;

[Description("Материалы по Id")]
public sealed record GetMaterialsByIdsForCalculateQuery(
    [property: Description("Id материалов")] int[] MaterialIds)
    : IQuery<GetMaterialsByIdsForCalculateQueryResult>;

public sealed class GetMaterialsByIdsForCalculateQueryValidator : AbstractValidator<GetMaterialsByIdsForCalculateQuery>
{
    public GetMaterialsByIdsForCalculateQueryValidator()
    {
        RuleFor(x => x.MaterialIds)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Не выбраны материалы")
            .Must(ids => ids.All(id => id > 0)).WithMessage("Id материалов должны быть больше нуля")
            .Must(ids => ids.Distinct().Count() == ids.Length).WithMessage("Id материалов не должны повторяться");
    }
}

[Description("Материалы")]
public sealed record GetMaterialsByIdsForCalculateQueryResult(
    [property: Description("Материалы")] GetMaterialsByIdsForCalculateQueryItem[] Items);

[Description("Материал")]
public sealed record GetMaterialsByIdsForCalculateQueryItem(
    [property: Description("Id")] int Id,
    [property: Description("Название")] string Name,
    [property: Description("Артикул")] string Article,
    [property: Description("Категория")] PropertyDto Category);

public sealed class GetMaterialsByIdsForCalculateQueryHandler(CatalogDbContext dbContext)
    : IQueryHandler<GetMaterialsByIdsForCalculateQuery, GetMaterialsByIdsForCalculateQueryResult>
{
    public async Task<GetMaterialsByIdsForCalculateQueryResult> Handle(GetMaterialsByIdsForCalculateQuery query, CancellationToken ct)
    {
        var items = await dbContext.Materials.AsNoTracking()
            .Where(x => query.MaterialIds.Contains(x.Id))
            .OrderBy(x => x.Category.OrderByCol)
            .ThenBy(x => x.OrderByCol)
            .Select(x => new GetMaterialsByIdsForCalculateQueryItem
            (
                x.Id,
                x.Name,
                x.Article,
                new PropertyDto(x.CategoryId, x.Category.Name)
            ))
            .ToArrayAsync(ct);

        return new GetMaterialsByIdsForCalculateQueryResult(items);
    }
}