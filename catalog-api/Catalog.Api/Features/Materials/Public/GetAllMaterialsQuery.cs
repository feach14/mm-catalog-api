using Catalog.Api.Features.Materials.Dto;
using Catalog.Api.Features.Materials.ForAdmin.Dto;
using Catalog.Database;
using Core.CQRS;

namespace Catalog.Api.Features.Materials.Public;

public sealed record GetAllMaterialsQuery(
    [property: Description("Калькулятор: Raskroy, PvhFacades или EmalFacades. Для выбора нескольких калькуляторов повторите параметр calculator. Без параметра возвращаются все материалы."), FromQuery] MaterialCalculatorEnum[]? Calculator,
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

public sealed record GetAllMaterialsQueryResult([property: Description("Список материалов")] GetAllMaterialsQueryItemResult[] Items);

public sealed record GetAllMaterialsQueryItemResult(
    [property: Description("Id материала")] int Id,
    [property: Description("Название материала")] string Name,
    [property: Description("Артикул материала")] string Article,
    [property: Description("Размер материала")] MaterialSheetSizeDto SheetSize,
    [property: Description("Производитель")] MaterialManufacturerDto Manufacturer,
    [property: Description("Толщина плиты")] double Depth,
    [property: Description("Категория")] CategoryDto Category,
    [property: Description("Количество")] int Count,
    [property: Description("Признак: Комментарий к материалу обязателен при оформлении заявки(расчета)")] bool CommentOnMaterialIsRequired,
    [property: Description("Порядковый номер записи (для сортировки)")] int OrderByCol);

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
                SheetSizeId = x.MaterialSheetSize.Id,
                SheetSizeName = x.MaterialSheetSize.Name,
                SheetSizeHeight = x.MaterialSheetSize.Height,
                SheetSizeWidth = x.MaterialSheetSize.Width,
                ManufacturerId = x.MaterialManufacturer.Id,
                ManufacturerName = x.MaterialManufacturer.Name,
                x.Depth,
                x.CategoryId,
                CategoryName = x.Category.Name,
                x.Count,
                x.CommentOnMaterialIsRequired,
                x.OrderByCol
            })
            .ToArrayAsync(ct);

        var materials = materialRows
            .Select(x => new GetAllMaterialsQueryItemResult(
                Id: x.Id,
                Name: x.Name,
                Article: x.Article,
                SheetSize: new MaterialSheetSizeDto(x.SheetSizeId, x.SheetSizeName, x.SheetSizeHeight, x.SheetSizeWidth),
                Manufacturer: new MaterialManufacturerDto(x.ManufacturerId, x.ManufacturerName),
                Depth: x.Depth,
                Category: new CategoryDto(x.CategoryId, x.CategoryName),
                Count: x.Count,
                CommentOnMaterialIsRequired: x.CommentOnMaterialIsRequired,
                OrderByCol: x.OrderByCol))
            .ToArray();

        return new GetAllMaterialsQueryResult(materials);
    }
}
