namespace Catalog.Api.Features.Edges.ArticleReplacements.Dto;

public sealed record EdgeOldArticleDto(
    [property: Description("Id")] int Id,
    [property: Description("Старый артикул ВЕК")] string OldArticle);