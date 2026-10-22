namespace Catalog.Api.Features.Relations.ForAdmin.Dto;

public sealed record MaterialEdgeRelationDto(
    [property: Description("Id материала")] int MaterialId,
    [property: Description("Id кромки")] int EdgeId,
    [property: Description("Приоритет")] int Priority,
    [property: Description("Id маркировок")] int[] MarkingIds);

public sealed record MaterialEdgeRelationFiltersDto(
    [property: Description("Маркировки")] RelationMarkingOptionDto[] Markings);

public sealed record RelationMarkingOptionDto(
    [property: Description("Id")] int Id,
    [property: Description("Название")] string Name);