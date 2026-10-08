// ReSharper disable NotAccessedPositionalProperty.Global

using Catalog.Database;
using Core.CQRS;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Catalog.Api.Features.Materials.ForCalculate;

public sealed record GetMaterialsForCalculateQuery(
    [property: Description("Id категории"), FromQuery, BindRequired] int CategoryId)
    : IQuery<GetMaterialsForCalculateQueryResult>;

public sealed class GetMaterialsForCalculateQueryValidator : AbstractValidator<GetMaterialsForCalculateQuery>
{
    public GetMaterialsForCalculateQueryValidator(CatalogDbContext dbContext)
    {
        RuleFor(x => x.CategoryId)
            .Cascade(CascadeMode.Stop)
            .GreaterThan(0).WithMessage("Не указана категория материалов или её Id не является положительным числом")
            .MustAsync(async (categoryId, ct) => await dbContext.MaterialCategories.AsNoTracking().AnyAsync(x => x.Id == categoryId, ct))
            .WithMessage("Указанная категория материалов не найдена");
    }
}

public sealed record GetMaterialsForCalculateQueryResult(
    [property: Description("Список материалов указанной категории")] GetMaterialsForCalculateQueryItem[] Items);

public sealed record GetMaterialsForCalculateQueryItem(
    [property: Description("Id")] int Id,
    [property: Description("Название")] string Name,
    [property: Description("Артикул")] string Article);

public sealed class GetMaterialsForCalculateQueryHandler(CatalogDbContext dbContext)
    : IQueryHandler<GetMaterialsForCalculateQuery, GetMaterialsForCalculateQueryResult>
{
    public async Task<GetMaterialsForCalculateQueryResult> Handle(GetMaterialsForCalculateQuery query, CancellationToken ct)
    {
        var items = await dbContext.Materials.AsNoTracking()
            .Where(x => x.CategoryId == query.CategoryId)
            .OrderBy(x => x.OrderByCol)
            .ThenBy(x => x.Id)
            .Select(x => new GetMaterialsForCalculateQueryItem(x.Id, x.Name, x.Article))
            .ToArrayAsync(ct);

        return new GetMaterialsForCalculateQueryResult(items);
    }
}