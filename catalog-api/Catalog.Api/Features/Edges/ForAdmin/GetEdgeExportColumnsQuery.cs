using Catalog.Api.Features.Exports.Dto;
using Core.CQRS;

namespace Catalog.Api.Features.Edges.ForAdmin;

public sealed record GetEdgeExportColumnsQuery : IQuery<ExcelExportColumnsResult>;

public sealed class GetEdgeExportColumnsQueryHandler : IQueryHandler<GetEdgeExportColumnsQuery, ExcelExportColumnsResult>
{
    internal static readonly IReadOnlyList<ExcelExportFieldDto> Fields =
    [
        new("article", "Артикул", true),
        new("name", "Название", true),
        new("price", "Стоимость метра/бухты, руб.", true),
        new("manufacturer", "Производитель", true),
        new("supplier", "Поставщик", true),
        new("thickness", "Толщина, мм", true),
        new("width", "Ширина, мм", true),
        new("saleUnit", "Единица продажи", true),
        new("saleUnitLength", "Длина единицы продажи, м", true),
        new("isStockItem", "Складская позиция", true),
        new("stockQuantity", "Остаток, м", true),
        new("id", "Id", false),
        new("supplierId", "Id поставщика", false),
        new("sizeName", "Название размера", false),
        new("saleUnitType", "Способ продажи", false),
        new("orderByCol", "Порядок сортировки", false),
        new("stroydvorProductId", "Id товара Стройдвора", false),
        new("stroydvorArticle", "Артикул Стройдвора", false),
        new("stroydvorPrice", "Цена Стройдвора за метр/бухту, руб.", false),
        new("stroydvorLastSyncAt", "Дата синхронизации со Стройдвором", false),
        new("stroydvorInStock", "В наличии в Стройдворе", false),
        new("stroydvorUrl", "Ссылка Стройдвора", false)
    ];

    public Task<ExcelExportColumnsResult> Handle(GetEdgeExportColumnsQuery query, CancellationToken ct) =>
        Task.FromResult(new ExcelExportColumnsResult(Fields));
}