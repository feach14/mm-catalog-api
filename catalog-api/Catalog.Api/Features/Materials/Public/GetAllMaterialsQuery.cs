namespace Catalog.Api.Features.Materials.Public;

using Core.CQRS;
using Database;
using Dto;
using Materials.Dto;

public sealed record GetAllMaterialsQuery(
    [property: Description("Признак: Добавить в выдачу материалы для кальулятора раскроя")] [property: FromQuery(Name = "raskroy")] bool Raskroy,
    [property: Description("Признак: Добавить в выдачу материалы для калькулятора фасадов ПВХ")] [property: FromQuery(Name = "pvhFacades")] bool PvhFacades,
    [property: Description("Признак: Добавить в выдачу материалы для калькулятора фасадов эмаль")] [property: FromQuery(Name = "emalFacades")] bool EmalFacades
) : IQuery<GetAllMaterialsQueryResult>;

public sealed record GetAllMaterialsQueryResult(
    [property: Description("Список материалов")] List<GetMaterialQueryResult> Materials,
    [property: Description("Список категорий из списка материалов")] List<MaterialCategoryDto> Categories);

public record MaterialCategoryDto(
    [property:Description("Id категории")] int Id,
    [property:Description("Название категории")] string Name,
    [property:Description("Порядковый номер для сортировки")] int OrderByCol);

public class GetAllMaterialsQueryHandler(CatalogDbContext dbContext) : IQueryHandler<GetAllMaterialsQuery, GetAllMaterialsQueryResult>
{
    public async Task<GetAllMaterialsQueryResult> Handle(GetAllMaterialsQuery query, CancellationToken ct)
    {
        var materialRows = await dbContext.Materials
            .Where(x =>
                ((query.Raskroy == true && x.ApplicableToRaskroys)
                 || (query.PvhFacades == true && x.ApplicableToPvhFacades)
                 || (query.EmalFacades == true && x.ApplicableToEmalFacades))
                && !x.Deleted)
            .OrderBy(x => x.Category.OrderByCol)
                .ThenBy(x => x.OrderByCol)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.Article,
                SheetSize = new SheetSizeDto(x.MaterialSheetSize.Id, x.MaterialSheetSize.Name, x.MaterialSheetSize.Height, x.MaterialSheetSize.Width),
                Manufacturer = new ManufacturerDto(x.MaterialManufacturer.Id, x.MaterialManufacturer.Name),
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
