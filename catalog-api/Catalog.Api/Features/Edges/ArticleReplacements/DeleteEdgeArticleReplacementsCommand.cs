using Catalog.Api.Services.History;
using Catalog.Database;
using Catalog.Database.Enums;
using Core.CQRS;

namespace Catalog.Api.Features.Edges.ArticleReplacements;

public sealed record DeleteEdgeArticleReplacementsCommand(int EdgeId) : ICommand<DeleteEdgeArticleReplacementsCommandResult>;

public sealed record DeleteEdgeArticleReplacementsCommandResult(
    [property: Description("Успех операции")] bool Success);

public sealed class DeleteEdgeArticleReplacementsCommandHandler(CatalogDbContext dbContext, ICatalogHistoryWriter historyWriter)
    : ICommandHandler<DeleteEdgeArticleReplacementsCommand, DeleteEdgeArticleReplacementsCommandResult>
{
    public async Task<DeleteEdgeArticleReplacementsCommandResult> Handle(DeleteEdgeArticleReplacementsCommand command, CancellationToken ct)
    {
        var replacements = await dbContext.EdgeArticleReplacements.Include(x => x.Edge)
            .Where(x => x.EdgeId == command.EdgeId).OrderBy(x => x.OldArticle).ThenBy(x => x.Id).ToArrayAsync(ct);
        if (replacements.Length == 0)
            throw new BadHttpRequestException("Замены артикулов этой кромки не найдены.");

        var edge = replacements[0].Edge;
        dbContext.EdgeArticleReplacements.RemoveRange(replacements);
        historyWriter.Add(
            CatalogHistoryActionTypeEnum.Update, CatalogHistoryEntityTypeEnum.Edge, edge.Id,
            $"Удалены замены старых артикулов ВЕК для кромки #{edge.Id} «{edge.Name}»: {string.Join(", ", replacements.Select(x => $"«{x.OldArticle}»"))}. Текущий артикул: {edge.Article}.");
        await dbContext.SaveChangesAsync(ct);
        return new DeleteEdgeArticleReplacementsCommandResult(true);
    }
}