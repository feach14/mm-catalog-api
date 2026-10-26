using Catalog.Api.Features.Edges.ArticleReplacements.Dto;
using Catalog.Database;
using Core.CQRS;

namespace Catalog.Api.Features.Edges.ArticleReplacements;

public sealed record GetEdgeArticleReplacementsQuery : IQuery<GetEdgeArticleReplacementsQueryResult>;

public sealed record GetEdgeArticleReplacementsQueryResult(
    [property: Description("Список замен артикулов")] GetEdgeArticleReplacementsQueryItem[] Items);

public sealed record GetEdgeArticleReplacementsQueryItem(
    [property: Description("Id кромки")] int EdgeId,
    [property: Description("Название кромки")] string EdgeName,
    [property: Description("Текущий артикул")] string Article,
    [property: Description("Толщина в миллиметрах")] decimal Thickness,
    [property: Description("Старые артикулы ВЕК")] EdgeOldArticleDto[] OldArticles);

public sealed class GetEdgeArticleReplacementsQueryHandler(CatalogDbContext dbContext)
    : IQueryHandler<GetEdgeArticleReplacementsQuery, GetEdgeArticleReplacementsQueryResult>
{
    public async Task<GetEdgeArticleReplacementsQueryResult> Handle(GetEdgeArticleReplacementsQuery query, CancellationToken ct)
    {
        var items = await dbContext.Edges.AsNoTracking()
            .Where(x => x.ArticleReplacements.Any())
            .OrderBy(x => x.Thickness.Value).ThenBy(x => x.Article).ThenBy(x => x.Id)
            .Select(x => new GetEdgeArticleReplacementsQueryItem(
                x.Id, x.Name, x.Article, x.Thickness.Value,
                x.ArticleReplacements.OrderBy(replacement => replacement.OldArticle).ThenBy(replacement => replacement.Id)
                    .Select(replacement => new EdgeOldArticleDto(replacement.Id, replacement.OldArticle)).ToArray()))
            .ToArrayAsync(ct);
        return new GetEdgeArticleReplacementsQueryResult(items);
    }
}