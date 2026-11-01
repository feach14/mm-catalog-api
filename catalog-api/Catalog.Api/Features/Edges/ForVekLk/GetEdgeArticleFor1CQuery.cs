using Catalog.Database;
using Core.CQRS;

namespace Catalog.Api.Features.Edges.ForVekLk;

[Description("Параметры артикула кромки для 1С")]
public sealed record GetEdgeArticleFor1CQuery(
    [property: Description("Текущий артикул")] string Article)
    : IQuery<GetEdgeArticleFor1CQueryResult>;

[Description("Артикул кромки для 1С")]
public sealed record GetEdgeArticleFor1CQueryResult(
    [property: Description("Артикул")] string Article);

public sealed class GetEdgeArticleFor1CQueryHandler(CatalogDbContext dbContext, IConfiguration configuration)
    : IQueryHandler<GetEdgeArticleFor1CQuery, GetEdgeArticleFor1CQueryResult>
{
    public async Task<GetEdgeArticleFor1CQueryResult> Handle(GetEdgeArticleFor1CQuery query, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(query.Article))
            throw new BadHttpRequestException("Укажите артикул кромки");
        if (query.Article.Length > 100)
            throw new BadHttpRequestException("Артикул кромки должен содержать не более 100 символов");

        var oldArticles = await dbContext.EdgeArticleReplacements.AsNoTracking()
            .Where(x => x.Edge.Article == query.Article)
            .OrderBy(x => x.Id)
            .Select(x => new { x.Id, x.OldArticle })
            .ToArrayAsync(ct);

        if (oldArticles.Length == 0)
            return new GetEdgeArticleFor1CQueryResult(query.Article);
        if (oldArticles.Length == 1)
            return new GetEdgeArticleFor1CQueryResult(oldArticles[0].OldArticle);

        var candidates = string.Join(", ", oldArticles.Select(article => $"«{article.OldArticle}»"));
        var selectedOldArticleId = configuration.GetSection("EdgeArticleFor1CSelections").GetValue<int?>(query.Article);
        if (selectedOldArticleId.HasValue)
        {
            var selectedArticle = oldArticles.FirstOrDefault(article => article.Id == selectedOldArticleId);
            if (selectedArticle == null)
                throw new BadHttpRequestException($"Для кромки с артикулом «{query.Article}» согласованный старый артикул с id={selectedOldArticleId} не найден. Варианты: {candidates}");

            return new GetEdgeArticleFor1CQueryResult(selectedArticle.OldArticle);
        }

        var articlesWithoutWhitespace = oldArticles.Where(article => !article.OldArticle.Any(char.IsWhiteSpace)).ToArray();
        if (articlesWithoutWhitespace.Length == 1)
            return new GetEdgeArticleFor1CQueryResult(articlesWithoutWhitespace[0].OldArticle);
        if (articlesWithoutWhitespace.Length == 0)
            return new GetEdgeArticleFor1CQueryResult(oldArticles[0].OldArticle);

        throw new BadHttpRequestException($"Для кромки с артикулом «{query.Article}» не удалось выбрать единственный старый артикул без пробелов. Варианты: {candidates}");
    }
}