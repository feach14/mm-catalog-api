using Catalog.Database;
using Core.CQRS;

namespace Catalog.Api.Features.Materials.ForAdmin;

public sealed record GetAllMaterialsForAdminQuery(
    [property: Description("Признак: Добавить в выдачу материалы для кальулятора раскроя")] [property: FromQuery(Name = "raskroy")] bool Raskroy,
    [property: Description("Признак: Добавить в выдачу материалы для калькулятора фасадов ПВХ")] [property: FromQuery(Name = "pvhFacades")] bool PvhFacades,
    [property: Description("Признак: Добавить в выдачу материалы для калькулятора фасадов эмаль")] [property: FromQuery(Name = "emalFacades")] bool EmalFacades,
    [property: Description("Id категории(коллекции) материала")] [property: FromQuery(Name = "categoryId")] int? CategoryId
) : IQuery<GetAllMaterialsForAdminQueryResult>;

public sealed record GetAllMaterialsForAdminQueryResult(
    [property: Description("Список материалов")] GetMaterialsQueryForAdminItemResult[] Materials);

public sealed record GetMaterialsQueryForAdminItemResult(
    [property: Description("Id материала")] int Id,
    [property: Description("Название материала")] string Name,
    [property: Description("Артикул материала")] string Article,
    [property: Description("Размер плиты")] string Size,
    [property: Description("Толщина плиты")] double Depth,
    [property: Description("Id категории")] int CategoryId,
    [property: Description("Название категории")] string CategoryName,
    [property: Description("Ссылка на внешний источник")] string? ExternalLink,
    [property: Description("Количество")] int Count,
    [property: Description("Порядковый номер записи (для сортировки)")] int OrderByCol,
    [property: Description("Стоимость материала")] decimal Price
);

public class GetAllMaterialsForAdminQueryHandler(CatalogDbContext dbContext) : IQueryHandler<GetAllMaterialsForAdminQuery, GetAllMaterialsForAdminQueryResult>
{
    public async Task<GetAllMaterialsForAdminQueryResult> Handle(GetAllMaterialsForAdminQuery query, CancellationToken ct)
    {
        var materials = (await dbContext.Materials
            .Where(x =>
                ((query.Raskroy == true && x.ApplicableToRaskroys)
                 || (query.PvhFacades == true && x.ApplicableToPvhFacades)
                 || (query.EmalFacades == true && x.ApplicableToEmalFacades))
                && (query.CategoryId == null || x.CategoryId == query.CategoryId.Value)
                && !x.Deleted)
            .OrderBy(x => x.Category.OrderByCol)
                .ThenBy(x => x.OrderByCol)
            .Select(x => new 
            {
                x.Id,
                x.Name,
                x.Article,
                x.Size,
                x.Depth,
                x.ExternalLink,
                x.CategoryId,
                x.Count,
                CategoryName = x.Category.Name,
                x.OrderByCol,
                x.Price
            })
            .ToArrayAsync(ct))
            .Select(x=> new GetMaterialsQueryForAdminItemResult(
                Id: x.Id,
                Name: x.Name,
                Article: x.Article,
                Size: x.Size,
                Depth: x.Depth,
                CategoryId: x.CategoryId,
                CategoryName: x.CategoryName,
                ExternalLink: x.ExternalLink,
                Count: x.Count,
                OrderByCol: x.OrderByCol,
                Price: x.Price
            ))
            .ToArray();

        return new GetAllMaterialsForAdminQueryResult(materials);
    }
}
