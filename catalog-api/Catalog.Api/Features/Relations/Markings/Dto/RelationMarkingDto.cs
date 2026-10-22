namespace Catalog.Api.Features.Relations.Markings.Dto;

[Description("Маркировка")]
public sealed record RelationMarkingDto(
    [property: Description("Id")] int Id,
    [property: Description("Название")] string Name,
    [property: Description("Количество связей")] int Count);