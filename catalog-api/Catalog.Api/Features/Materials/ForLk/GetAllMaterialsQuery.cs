using Catalog.Api.Features.Materials.ForLk.Dto;
using Catalog.Database;
using Core.CQRS;

namespace Catalog.Api.Features.Materials.ForLk;

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
        var materials = await dbContext.Materials
            .Include(x => x.Category)
            .Where(x =>
                ((query.Raskroy == true && x.ApplicableToRaskroys)
                 || (query.PvhFacades == true && x.ApplicableToPvhFacades)
                 || (query.EmalFacades == true && x.ApplicableToEmalFacades))
                && !x.Deleted)
            .Select(x => new GetMaterialQueryResult
            {
                Id = x.Id,
                Name = x.Name,
                Article = x.Article,
                Size = x.Size,
                Depth = x.Depth,
                CommentOnMaterialIsRequired = x.CommentOnMaterialIsRequired,
                Image = x.Images.Count != 0 ? x.Images.Select(g => g.Guid).First() : null,
                CategoryId = x.CategoryId,
                CategoryName = x.Category.Name,
                OrderByCol = x.OrderByCol,
                CategoryOrderBy = x.Category.OrderByCol,
                Count = x.Count
            })
            .OrderBy(x => x.CategoryOrderBy)
                .ThenBy(x => x.OrderByCol)
            .ToListAsync(ct);

        var categories = materials
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
