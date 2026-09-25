namespace Catalog.Api.Features.MaterialCategories.Dto;

public class MaterialCategoryForAdminDto
{
    [Description("Id категории")] 
    public required int Id { get; init; }

    [Description("Название категории")]
    public required string Name { get; init; }

    [Description("Ссылка на внешний источник")]
    public required string ExternalLink { get; init; }

    [Description("Порядковый номер для сортировки")]
    public required int OrderByCol { get; init; }

    [Description("Количество материалов в наличии у текущей категории")]
    public required int MaterialsAnyCount { get; init; }

    [Description("Количество материалов не в наличии у текущей категории")]
    public required int MaterialsNotAnyCount { get; init; }
}