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
        new("price", "Стоимость, руб./м", true),
        new("manufacturer", "Производитель", true),
        new("thickness", "Толщина, мм", true),
        new("saleUnit", "Единица продажи", true),
        new("saleUnitLength", "Длина единицы продажи, м", true),
        new("isStockItem", "Складская позиция", true),
        new("stockQuantity", "Остаток, м", true),
        new("id", "Id", false),
        new("thicknessName", "Название толщины", false),
        new("saleUnitType", "Способ продажи", false),
        new("orderByCol", "Порядок сортировки", false)
    ];

    public Task<ExcelExportColumnsResult> Handle(GetEdgeExportColumnsQuery query, CancellationToken ct) =>
        Task.FromResult(new ExcelExportColumnsResult(Fields));
}