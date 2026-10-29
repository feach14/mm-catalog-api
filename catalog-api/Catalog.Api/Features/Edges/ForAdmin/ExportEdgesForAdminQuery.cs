using Catalog.Api.Features.Exports.Dto;
using Catalog.Api.Infrastructure.Excel;
using Catalog.Database;
using Catalog.Database.Enums;
using Core.CQRS;

namespace Catalog.Api.Features.Edges.ForAdmin;

public sealed record ExportEdgesForAdminQuery(
    [property: Description("Поля и колонки выгрузки")] ExcelExportColumnDto[] Columns,
    [property: Description("Id в порядке выгрузки; null — все кромки, пустой список — без строк")] int[]? Ids = null,
    [property: Description("Добавить строку заголовков")] bool IncludeHeaders = true) : IQuery<ExcelExportFile>;

public sealed class ExportEdgesForAdminQueryValidator : AbstractValidator<ExportEdgesForAdminQuery>
{
    public ExportEdgesForAdminQueryValidator(CatalogDbContext dbContext)
    {
        RuleFor(x => x.Columns).Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Выберите хотя бы одно поле выгрузки.")
            .Must(columns => columns.Length <= 100).WithMessage("Можно выгрузить не более 100 колонок.")
            .Must(columns => columns.All(column => column is not null)).WithMessage("Не указаны параметры колонки.")
            .Must(columns => columns.Select(column => column.Key).Distinct().Count() == columns.Length).WithMessage("Поля выгрузки не должны повторяться.")
            .Must(columns => columns.Select(column => column.Column).Distinct().Count() == columns.Length).WithMessage("Нельзя назначить несколько полей одной колонке.")
            .Must(columns => columns.All(column => GetEdgeExportColumnsQueryHandler.Fields.Any(field => field.Key == column.Key))).WithMessage("Указано неизвестное поле выгрузки кромок.");
        RuleForEach(x => x.Columns).SetValidator(new ExcelExportColumnDtoValidator());
        RuleFor(x => x.Ids).Cascade(CascadeMode.Stop)
            .Must(ids => ids is null || ids.All(id => id > 0)).WithMessage("Id кромок должны быть положительными числами.")
            .Must(ids => ids is null || ids.Distinct().Count() == ids.Length).WithMessage("Id кромок не должны повторяться.")
            .Must((query, ids) => ids is null || ids.Length <= 1048576 - (query.IncludeHeaders ? 1 : 0)).WithMessage("Количество кромок превышает ограничение Excel.")
            .MustAsync(async (ids, ct) => ids is null || ids.Length == 0
                || await dbContext.Edges.AsNoTracking().CountAsync(x => ids.Contains(x.Id), ct) == ids.Length).WithMessage("Одна или несколько кромок не найдены. Обновите список.");
    }
}

public sealed class ExportEdgesForAdminQueryHandler(CatalogDbContext dbContext)
    : IQueryHandler<ExportEdgesForAdminQuery, ExcelExportFile>
{
    public async Task<ExcelExportFile> Handle(ExportEdgesForAdminQuery query, CancellationToken ct)
    {
        var edges = dbContext.Edges.AsNoTracking();
        if (query.Ids is not null)
            edges = edges.Where(x => query.Ids.Contains(x.Id));
        var rows = await edges.OrderBy(x => x.OrderByCol).ThenBy(x => x.Id)
            .Select(x => new
            {
                x.Id,
                x.Article,
                x.Name,
                x.Price,
                Manufacturer = x.Manufacturer.Name,
                Thickness = x.Thickness.Value,
                ThicknessName = x.Thickness.Name,
                SaleUnit = x.SaleUnit.Name,
                SaleUnitLength = x.SaleUnit.Length,
                SaleUnitType = x.SaleUnit.SaleUnitType,
                x.IsStockItem,
                x.StockQuantity,
                x.OrderByCol
            }).ToListAsync(ct);
        if (query.Ids is not null)
        {
            var positions = query.Ids.Select((id, index) => (id, index)).ToDictionary(x => x.id, x => x.index);
            rows.Sort((left, right) => positions[left.Id].CompareTo(positions[right.Id]));
        }
        var columns = query.Columns.OrderBy(x => x.Column).ToArray();
        var titles = GetEdgeExportColumnsQueryHandler.Fields.ToDictionary(x => x.Key, x => x.Title);
        return ExcelWorkbook.Create("Кромки", "edges.xlsx",
            columns.Select(x => new ExcelWorkbookColumn(x.Column, titles[x.Key])).ToArray(),
            rows.Select(row => columns.Select<ExcelExportColumnDto, object?>(column => column.Key switch
            {
                "id" => row.Id,
                "article" => row.Article,
                "name" => row.Name,
                "price" => row.Price,
                "manufacturer" => row.Manufacturer,
                "thickness" => row.Thickness,
                "thicknessName" => row.ThicknessName,
                "saleUnit" => row.SaleUnit,
                "saleUnitLength" => row.SaleUnitLength,
                "saleUnitType" => row.SaleUnitType switch
                {
                    EdgeSaleUnitTypeEnum.Meter => "Погонный метр",
                    EdgeSaleUnitTypeEnum.Roll => "Рулон",
                    _ => row.SaleUnitType.ToString()
                },
                "isStockItem" => row.IsStockItem,
                "stockQuantity" => row.StockQuantity,
                "orderByCol" => row.OrderByCol,
                _ => throw new InvalidOperationException("Неизвестное поле выгрузки кромок.")
            }).ToArray()), query.IncludeHeaders, ct);
    }
}