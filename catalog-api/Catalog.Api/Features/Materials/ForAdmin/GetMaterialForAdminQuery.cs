namespace Catalog.Api.Features.Materials.ForAdmin;

using Core.CQRS;
using Database;
using Database.Enums;
using Materials.Dto;

public sealed record GetMaterialForAdminQuery(int Id) : IQuery<GetMaterialForAdminQueryResult>;

public sealed record GetMaterialForAdminQueryResult(
    [property: Description("Id материала")] int Id,
    [property: Description("Название материала")] string Name,
    [property: Description("Артикул материала")] string Article,
    [property: Description("Размер материала")] SheetSizeDto SheetSize,
    [property: Description("Толщина плиты")] double Depth,
    [property: Description("Количество кв. м. в плите")] double KvM,
    [property: Description("Количество метров по периметру плиты")] double PerimetrM,
    [property: Description("Использование материала в калькуляторе раскроя")] bool ApplicableToRaskroys,
    [property: Description("Использование материала в калькуляторе фасадов ПВХ")] bool ApplicableToPvhFacades,
    [property: Description("Использование материала в калькуляторе фасадов эмаль")] bool ApplicableToEmalFacades,
    [property: Description("Id изображения материала")] Guid? Image,
    [property: Description("Категория")] CategoryDto Category,
    [property: Description("Количество")] int Count,
    [property: Description("Порядковый номер записи (для сортировки)")] int OrderByCol,
    [property: Description("Признак: Комментарий к материалу обязателен при оформлении заявки(расчета)")] bool CommentOnMaterialIsRequired,
    [property: Description("Признак: Разрешено добавлять вторым(и более) элементом списка расчетов в заявке")] bool AllowSecondItemInOrder,
    [property: Description("Ссылка на внешний источник")] string? ExternalLink,
    [property: Description("Цена за материал у поставщика")] decimal Price,
    [property: Description("Единица измерения"), JsonConverter(typeof(JsonStringEnumConverter))] CountTypeEnum CountTypeEnum
);

public class GetMaterialForAdminQueryHandler(CatalogDbContext dbContext) : IQueryHandler<GetMaterialForAdminQuery, GetMaterialForAdminQueryResult>
{
    public async Task<GetMaterialForAdminQueryResult> Handle(GetMaterialForAdminQuery query, CancellationToken ct)
    {
        var queryResult = await dbContext.Materials
                   .Where(x => x.Id == query.Id)
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
                       x.Depth,
                       x.KvM,
                       x.PerimetrM,
                       x.ApplicableToRaskroys,
                       x.ApplicableToPvhFacades,
                       x.ApplicableToEmalFacades,
                       x.CommentOnMaterialIsRequired,
                       x.AllowSecondItemInOrder,
                       x.ExternalLink,
                       Image = x.Images.Count != 0 ? x.Images.Select(g => g.Guid).First() : (Guid?)null,
                       x.CategoryId,
                       CategoryName = x.Category.Name,
                       x.Count,
                       x.OrderByCol,
                       x.Price,
                       x.CountTypeEnum
                   })
                   .SingleOrDefaultAsync(ct)
               ?? throw new BadHttpRequestException($"Материал с id={query.Id} не найден");

        return new GetMaterialForAdminQueryResult(
            Id: queryResult.Id,
            Name: queryResult.Name,
            Article: queryResult.Article,
            SheetSize: queryResult.SheetSize,
            Depth: queryResult.Depth,
            KvM: queryResult.KvM,
            PerimetrM: queryResult.PerimetrM,
            ApplicableToRaskroys: queryResult.ApplicableToRaskroys,
            ApplicableToPvhFacades: queryResult.ApplicableToPvhFacades,
            ApplicableToEmalFacades: queryResult.ApplicableToEmalFacades,
            Image: queryResult.Image,
            Category: new CategoryDto(queryResult.CategoryId, queryResult.CategoryName),
            Count: queryResult.Count,
            OrderByCol: queryResult.OrderByCol,
            CommentOnMaterialIsRequired: queryResult.CommentOnMaterialIsRequired,
            AllowSecondItemInOrder: queryResult.AllowSecondItemInOrder,
            ExternalLink: queryResult.ExternalLink,
            Price: queryResult.Price,
            CountTypeEnum: queryResult.CountTypeEnum
        );
    }
}
