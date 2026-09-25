using System.Diagnostics.CodeAnalysis;

namespace Catalog.Api.Features.Materials.Public.Dto;

[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
public sealed record GetMaterialQueryResult
{
    [Description("Id материала")]
    public int Id { get; init; }
    
    [Description("Название материала")]
    public required string Name { get; init; }
    
    [Description("Артикул материала")]
    public required string Article { get; init; }
    
    [Description("Размер плиты")]
    public required string Size { get; init; }
    
    [Description("Толщина плиты")]
    public required double Depth { get; init; }
    
    [Description("Id изображения материала")]
    public required Guid? Image { get; init; }
    
    [Description("Id категории")]
    public required int CategoryId { get; init; }
    
    [Description("Порядковый номер категории (для сортировки)")]
    public required int CategoryOrderBy { get; init; }
    
    [Description("Название категории")]
    public required string CategoryName { get; init; }
    
    [Description("Количество")]
    public required int Count { get; init; }
    
    [Description("Признак: Комментарий к материалу обязателен при оформлении заявки(расчета)")]
    public bool CommentOnMaterialIsRequired { get; init; }
    
    [Description("Порядковый номер записи (для сортировки)")]
    public required int OrderByCol { get; init; }
}
