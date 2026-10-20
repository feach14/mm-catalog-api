// ReSharper disable NotAccessedPositionalProperty.Global

using Core.CQRS;

namespace Catalog.Api.Features.Materials.ForAdmin;

public sealed record GetCompactMaterialsForAdminQuery : IQuery<GetCompactMaterialsForAdminQueryResult>;

public sealed record GetCompactMaterialsForAdminQueryResult(
    [property: Description("Список материалов")] GetCompactMaterialsForAdminQueryItem[] Items,
    [property: Description("Справочники для фильтров и редактора")] MaterialsForAdminFiltersDto Filters);

public sealed record GetCompactMaterialsForAdminQueryItem(
    [property: Description("Id")] int I,
    [property: Description("Название")] string N,
    [property: Description("Артикул")] string A,
    [property: Description("Id формата плиты")] int S,
    [property: Description("Id производителя")] int M,
    [property: Description("Id типа материала")] int T,
    [property: Description("Id толщины")] int Th,
    [property: Description("Id категории")] int C,
    [property: Description("Количество")] int Q,
    [property: Description("Порядок сортировки")] int O,
    [property: Description("Стоимость")] decimal P,
    [property: Description("Скрыть на сайте")] bool H,
    [property: Description("Не показывать цену на сайте")] bool Hp,
    [property: Description("Применим к распилу")] bool Cu,
    [property: Description("Применим к фасадам ПВХ")] bool Pv,
    [property: Description("Применим к фасадам эмаль")] bool En);

public sealed class GetCompactMaterialsForAdminQueryHandler(
    IQueryHandler<GetAllMaterialsForAdminQuery, GetAllMaterialsForAdminQueryResult> materialsHandler)
    : IQueryHandler<GetCompactMaterialsForAdminQuery, GetCompactMaterialsForAdminQueryResult>
{
    public async Task<GetCompactMaterialsForAdminQueryResult> Handle(GetCompactMaterialsForAdminQuery query, CancellationToken ct)
    {
        var materials = await materialsHandler.Handle(new GetAllMaterialsForAdminQuery(), ct);
        var items = materials.Items.Select(x => new GetCompactMaterialsForAdminQueryItem(
            x.Id,
            x.Name,
            x.Article,
            x.SheetSize.Id,
            x.Manufacturer.Id,
            x.MaterialType.Id,
            x.Thickness.Id,
            x.Category.Id,
            x.Count,
            x.OrderByCol,
            x.Price,
            x.HideOnSite,
            x.HidePriceOnSite,
            x.ApplicableToCutting,
            x.ApplicableToPvhFacades,
            x.ApplicableToEnamelFacades)).ToArray();

        return new GetCompactMaterialsForAdminQueryResult(items, materials.Filters);
    }
}