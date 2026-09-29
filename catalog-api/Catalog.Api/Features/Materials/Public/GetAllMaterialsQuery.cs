using Catalog.Api.Features.Materials.Dto;
using Catalog.Api.Features.Materials.Public.Dto;
using Catalog.Database;
using Core.CQRS;

namespace Catalog.Api.Features.Materials.Public;

public sealed record GetAllMaterialsQuery(
    [property: Description("Калькулятор: Raskroy, PvhFacades или EmalFacades. Для выбора нескольких калькуляторов повторите параметр calculator. Без параметра возвращаются все материалы."), FromQuery] MaterialCalculator[]? Calculator,
    [property: Description("Наличие материала: true — count > 0, false — count < 1. Без параметра возвращаются все материалы."), FromQuery] bool? InStock
) : IQuery<GetAllMaterialsQueryResult>;

public sealed class GetAllMaterialsQueryValidator : AbstractValidator<GetAllMaterialsQuery>
{
    public GetAllMaterialsQueryValidator()
    {
        RuleForEach(x => x.Calculator!)
            .IsInEnum()
            .When(x => x.Calculator is not null)
            .WithMessage("Указан недопустимый калькулятор");
    }
}

public sealed record GetAllMaterialsQueryResult(
    [property: Description("Список материалов")] List<GetMaterialQueryResult> Items,
    [property: Description("Список категорий из списка материалов")] List<MaterialCategoryDto> Categories);

public record MaterialCategoryDto(
    [property: Description("Id категории")] int Id,
    [property: Description("Название категории")] string Name,
    [property: Description("Порядковый номер для сортировки")] int OrderByCol);

public class GetAllMaterialsQueryHandler(CatalogDbContext dbContext) : IQueryHandler<GetAllMaterialsQuery, GetAllMaterialsQueryResult>
{
    public async Task<GetAllMaterialsQueryResult> Handle(GetAllMaterialsQuery query, CancellationToken ct)
    {
        var materialsQuery = dbContext.Materials.FilterByCalculators(query.Calculator);

        if (query.InStock.HasValue)
            materialsQuery = query.InStock.Value
                ? materialsQuery.Where(x => x.Count > 0)
                : materialsQuery.Where(x => x.Count < 1);

        var materialRows = await materialsQuery
            .OrderBy(x => x.Category.OrderByCol)
                .ThenBy(x => x.OrderByCol)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.Article,
                SheetSize = new MaterialSheetSizeDto(x.MaterialSheetSize.Id, x.MaterialSheetSize.Name, x.MaterialSheetSize.Height, x.MaterialSheetSize.Width),
                Manufacturer = new MaterialManufacturerDto(x.MaterialManufacturer.Id, x.MaterialManufacturer.Name),
                x.Depth,
                x.CommentOnMaterialIsRequired,
                Image = x.Images.Count != 0 ? x.Images.Select(g => g.Guid).First() : (Guid?)null,
                x.CategoryId,
                CategoryName = x.Category.Name,
                x.OrderByCol,
                CategoryOrderBy = x.Category.OrderByCol,
                x.Count
            })
            .ToListAsync(ct);

        var materials = materialRows
            .Select(x => new GetMaterialQueryResult
            {
                Id = x.Id,
                Name = x.Name,
                Article = x.Article,
                SheetSize = x.SheetSize,
                Manufacturer = x.Manufacturer,
                Depth = x.Depth,
                CommentOnMaterialIsRequired = x.CommentOnMaterialIsRequired,
                Image = x.Image,
                Category = new CategoryDto(x.CategoryId, x.CategoryName),
                OrderByCol = x.OrderByCol,
                Count = x.Count
            })
            .ToList();

        var categories = materialRows
            .GroupBy(x => new { x.CategoryId, x.CategoryName, x.CategoryOrderBy })
            .Select(x => new MaterialCategoryDto
            (
                Id: x.Key.CategoryId,
                Name: x.Key.CategoryName,
                OrderByCol: x.Key.CategoryOrderBy
            ))
            .OrderBy(x => x.OrderByCol)
            .ToList();

        return new GetAllMaterialsQueryResult(materials, categories);
    }
}