using Catalog.Api.Features.Materials;
using Catalog.Api.Features.Materials.Dto;
using Catalog.Database;
using Core.CQRS;

namespace Catalog.Api.Features.Materials.ForAdmin;

public sealed record GetAllMaterialsForAdminQuery(
    [property: Description("Калькулятор: Raskroy, PvhFacades или EmalFacades. Для выбора нескольких калькуляторов повторите параметр calculator. Без параметра возвращаются все материалы.")][property: FromQuery(Name = "calculator")]
    MaterialCalculator[]? Calculator,
    [property: Description("Id категории(коллекции) материала")][property: FromQuery(Name = "categoryId")] int? CategoryId
) : IQuery<GetAllMaterialsForAdminQueryResult>;

public sealed class GetAllMaterialsForAdminQueryValidator : AbstractValidator<GetAllMaterialsForAdminQuery>
{
    public GetAllMaterialsForAdminQueryValidator()
    {
        RuleForEach(x => x.Calculator!)
            .IsInEnum()
            .When(x => x.Calculator is not null);
    }
}

public sealed record GetAllMaterialsForAdminQueryResult(
    [property: Description("Список материалов")] GetMaterialsQueryForAdminItemResult[] Materials);

public sealed record GetMaterialsQueryForAdminItemResult(
    [property: Description("Id материала")] int Id,
    [property: Description("Название материала")] string Name,
    [property: Description("Артикул материала")] string Article,
    [property: Description("Размер материала")] SheetSizeDto SheetSize,
    [property: Description("Производитель")] ManufacturerDto Manufacturer,
    [property: Description("Толщина плиты")] double Depth,
    [property: Description("Категория")] CategoryDto Category,
    [property: Description("Ссылка на внешний источник")] string? ExternalLink,
    [property: Description("Количество")] int Count,
    [property: Description("Порядковый номер записи (для сортировки)")] int OrderByCol,
    [property: Description("Стоимость материала")] decimal Price
);

public class GetAllMaterialsForAdminQueryHandler(CatalogDbContext dbContext) : IQueryHandler<GetAllMaterialsForAdminQuery, GetAllMaterialsForAdminQueryResult>
{
    public async Task<GetAllMaterialsForAdminQueryResult> Handle(GetAllMaterialsForAdminQuery query, CancellationToken ct)
    {
        var materialsQuery = dbContext.Materials
            .Where(x => !x.Deleted)
            .FilterByCalculators(query.Calculator);

        if (query.CategoryId.HasValue)
            materialsQuery = materialsQuery.Where(x => x.CategoryId == query.CategoryId.Value);

        var materials = (await materialsQuery
            .OrderBy(x => x.Category.OrderByCol)
                .ThenBy(x => x.OrderByCol)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.Article,
                SheetSize = new SheetSizeDto(
                    x.MaterialSheetSize.Id,
                    x.MaterialSheetSize.Name,
                    x.MaterialSheetSize.Height,
                    x.MaterialSheetSize.Width),
                Manufacturer = new ManufacturerDto(x.MaterialManufacturer.Id, x.MaterialManufacturer.Name),
                x.Depth,
                x.ExternalLink,
                x.CategoryId,
                x.Count,
                CategoryName = x.Category.Name,
                x.OrderByCol,
                x.Price
            })
            .ToArrayAsync(ct))
            .Select(x => new GetMaterialsQueryForAdminItemResult(
                Id: x.Id,
                Name: x.Name,
                Article: x.Article,
                SheetSize: x.SheetSize,
                Manufacturer: x.Manufacturer,
                Depth: x.Depth,
                Category: new CategoryDto(x.CategoryId, x.CategoryName),
                ExternalLink: x.ExternalLink,
                Count: x.Count,
                OrderByCol: x.OrderByCol,
                Price: x.Price
            ))
            .ToArray();

        return new GetAllMaterialsForAdminQueryResult(materials);
    }
}