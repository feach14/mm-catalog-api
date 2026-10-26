using Catalog.Api.Features.Exports.Dto;
using Catalog.Api.Infrastructure.Excel;
using Catalog.Database;
using Core.BaseEnums;
using Core.CQRS;

namespace Catalog.Api.Features.Materials.ForAdmin;

public sealed record ExportMaterialsForAdminQuery(
    [property: Description("Поля и колонки выгрузки")] ExcelExportColumnDto[] Columns,
    [property: Description("Id в порядке выгрузки; null — все материалы, пустой список — без строк")] int[]? Ids = null,
    [property: Description("Добавить строку заголовков")] bool IncludeHeaders = true) : IQuery<ExcelExportFile>;

public sealed class ExportMaterialsForAdminQueryValidator : AbstractValidator<ExportMaterialsForAdminQuery>
{
    public ExportMaterialsForAdminQueryValidator(CatalogDbContext dbContext)
    {
        RuleFor(x => x.Columns).Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Выберите хотя бы одно поле выгрузки.")
            .Must(columns => columns.Length <= 100).WithMessage("Можно выгрузить не более 100 колонок.")
            .Must(columns => columns.All(column => column is not null)).WithMessage("Не указаны параметры колонки.")
            .Must(columns => columns.Select(column => column.Key).Distinct().Count() == columns.Length).WithMessage("Поля выгрузки не должны повторяться.")
            .Must(columns => columns.Select(column => column.Column).Distinct().Count() == columns.Length).WithMessage("Нельзя назначить несколько полей одной колонке.")
            .Must(columns => columns.All(column => GetMaterialExportColumnsQueryHandler.Fields.Any(field => field.Key == column.Key))).WithMessage("Указано неизвестное поле выгрузки материалов.");
        RuleForEach(x => x.Columns).SetValidator(new ExcelExportColumnDtoValidator());
        RuleFor(x => x.Ids).Cascade(CascadeMode.Stop)
            .Must(ids => ids is null || ids.All(id => id > 0)).WithMessage("Id материалов должны быть положительными числами.")
            .Must(ids => ids is null || ids.Distinct().Count() == ids.Length).WithMessage("Id материалов не должны повторяться.")
            .Must((query, ids) => ids is null || ids.Length <= 1048576 - (query.IncludeHeaders ? 1 : 0)).WithMessage("Количество материалов превышает ограничение Excel.")
            .MustAsync(async (ids, ct) => ids is null || ids.Length == 0
                || await dbContext.Materials.AsNoTracking().CountAsync(x => ids.Contains(x.Id), ct) == ids.Length).WithMessage("Один или несколько материалов не найдены. Обновите список.");
    }
}

public sealed class ExportMaterialsForAdminQueryHandler(CatalogDbContext dbContext)
    : IQueryHandler<ExportMaterialsForAdminQuery, ExcelExportFile>
{
    public async Task<ExcelExportFile> Handle(ExportMaterialsForAdminQuery query, CancellationToken ct)
    {
        var materials = dbContext.Materials.AsNoTracking();
        if (query.Ids is not null)
            materials = materials.Where(x => query.Ids.Contains(x.Id));
        var rows = await materials.OrderBy(x => x.OrderByCol).ThenBy(x => x.Id)
            .Select(x => new
            {
                x.Id,
                x.Article,
                x.Name,
                Category = x.Category.Name,
                Manufacturer = x.MaterialManufacturer.Name,
                MaterialType = x.MaterialType.Name,
                Thickness = x.MaterialThickness.Value,
                ThicknessName = x.MaterialThickness.Name,
                SheetSize = x.MaterialSheetSize.Name,
                x.MaterialSheetSize.Width,
                x.MaterialSheetSize.Height,
                x.Price,
                x.Count,
                x.CountTypeEnum,
                x.ExternalLink,
                x.StroydvorProductId,
                x.StroydvorArticle,
                x.StroydvorPrice,
                x.StroydvorLastSyncAt,
                x.StroydvorInStock,
                x.HideOnSite,
                x.HidePriceOnSite,
                x.ApplicableToCutting,
                x.ApplicableToPvhFacades,
                x.ApplicableToEnamelFacades,
                x.CommentOnMaterialIsRequired,
                x.AllowSecondItemInOrder,
                x.OrderByCol
            }).ToListAsync(ct);
        if (query.Ids is not null)
        {
            var positions = query.Ids.Select((id, index) => (id, index)).ToDictionary(x => x.id, x => x.index);
            rows.Sort((left, right) => positions[left.Id].CompareTo(positions[right.Id]));
        }
        var columns = query.Columns.OrderBy(x => x.Column).ToArray();
        var titles = GetMaterialExportColumnsQueryHandler.Fields.ToDictionary(x => x.Key, x => x.Title);
        return ExcelWorkbook.Create("Материалы", "materials.xlsx",
            columns.Select(x => new ExcelWorkbookColumn(x.Column, titles[x.Key])).ToArray(),
            rows.Select(row => columns.Select<ExcelExportColumnDto, object?>(column => column.Key switch
            {
                "id" => row.Id,
                "article" => row.Article,
                "name" => row.Name,
                "category" => row.Category,
                "manufacturer" => row.Manufacturer,
                "materialType" => row.MaterialType,
                "thickness" => row.Thickness,
                "thicknessName" => row.ThicknessName,
                "sheetSize" => row.SheetSize,
                "width" => row.Width,
                "height" => row.Height,
                "price" => row.Price,
                "count" => row.Count,
                "countType" => row.CountTypeEnum switch
                {
                    CountTypeEnum.M2 => "м²",
                    CountTypeEnum.PM => "пог. м",
                    CountTypeEnum.SHT => "шт.",
                    CountTypeEnum.PERCENT => "%",
                    CountTypeEnum.LIST => "лист",
                    CountTypeEnum.PACK => "упаковка",
                    CountTypeEnum.FIX => "фиксированная",
                    CountTypeEnum.KG => "кг",
                    _ => row.CountTypeEnum.ToString()
                },
                "externalLink" => row.ExternalLink,
                "stroydvorProductId" => row.StroydvorProductId,
                "stroydvorArticle" => row.StroydvorArticle,
                "stroydvorPrice" => row.StroydvorPrice,
                "stroydvorLastSyncAt" => row.StroydvorLastSyncAt?.ToString("O"),
                "stroydvorInStock" => row.StroydvorInStock,
                "stroydvorUrl" => row.StroydvorProductId is { } productId ? $"https://stroydvor21.ru/products/{productId}" : null,
                "hideOnSite" => row.HideOnSite,
                "hidePriceOnSite" => row.HidePriceOnSite,
                "applicableToCutting" => row.ApplicableToCutting,
                "applicableToPvhFacades" => row.ApplicableToPvhFacades,
                "applicableToEnamelFacades" => row.ApplicableToEnamelFacades,
                "commentOnMaterialIsRequired" => row.CommentOnMaterialIsRequired,
                "allowSecondItemInOrder" => row.AllowSecondItemInOrder,
                "orderByCol" => row.OrderByCol,
                _ => throw new InvalidOperationException("Неизвестное поле выгрузки материалов.")
            }).ToArray()), query.IncludeHeaders, ct);
    }
}