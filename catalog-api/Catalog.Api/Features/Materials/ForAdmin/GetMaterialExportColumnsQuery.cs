using Catalog.Api.Features.Exports.Dto;
using Core.CQRS;

namespace Catalog.Api.Features.Materials.ForAdmin;

public sealed record GetMaterialExportColumnsQuery : IQuery<ExcelExportColumnsResult>;

public sealed class GetMaterialExportColumnsQueryHandler : IQueryHandler<GetMaterialExportColumnsQuery, ExcelExportColumnsResult>
{
    internal static readonly IReadOnlyList<ExcelExportFieldDto> Fields =
    [
        new("article", "Артикул", true),
        new("name", "Название", true),
        new("category", "Категория", true),
        new("manufacturer", "Производитель", true),
        new("materialType", "Тип материала", true),
        new("thickness", "Толщина, мм", true),
        new("width", "Ширина, мм", true),
        new("height", "Высота, мм", true),
        new("price", "Цена", true),
        new("count", "Количество", true),
        new("countType", "Единица измерения", true),
        new("id", "Id", false),
        new("sheetSize", "Формат плиты", false),
        new("thicknessName", "Название толщины", false),
        new("externalLink", "Внешняя ссылка", false),
        new("stroydvorProductId", "Id товара Стройдвора", false),
        new("stroydvorArticle", "Артикул Стройдвора", false),
        new("stroydvorPrice", "Цена Стройдвора", false),
        new("stroydvorLastSyncAt", "Синхронизация со Стройдвором, UTC", false),
        new("stroydvorInStock", "В наличии в Стройдворе", false),
        new("stroydvorUrl", "Техническая ссылка Стройдвора", false),
        new("hideInLk", "Скрыт в ЛК", false),
        new("hideOnSite", "Скрыт на сайте", false),
        new("hidePriceOnSite", "Цена скрыта на сайте", false),
        new("commentOnMaterialIsRequired", "Комментарий обязателен", false),
        new("allowSecondItemInOrder", "Разрешён повторный элемент в заявке", false),
        new("orderByCol", "Порядок сортировки", false)
    ];

    public Task<ExcelExportColumnsResult> Handle(GetMaterialExportColumnsQuery query, CancellationToken ct) =>
        Task.FromResult(new ExcelExportColumnsResult(Fields));
}