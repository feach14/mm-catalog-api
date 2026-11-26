using Catalog.Api.Infrastructure.Ordering;
using Catalog.Api.Services.History;
using Catalog.Database;
using Catalog.Database.Enums;
using Core.CQRS;

namespace Catalog.Api.Features.Edges.ForAdmin;

public sealed record DeleteEdgeCommentCommand(int Id, int CommentId) : ICommand<DeleteEdgeCommentCommandResult>;

public sealed record DeleteEdgeCommentCommandResult(
    [property: Description("Успех операции")] bool Success);

public sealed class DeleteEdgeCommentCommandHandler(CatalogDbContext dbContext, ICatalogHistoryWriter historyWriter)
    : ICommandHandler<DeleteEdgeCommentCommand, DeleteEdgeCommentCommandResult>
{
    public async Task<DeleteEdgeCommentCommandResult> Handle(DeleteEdgeCommentCommand command, CancellationToken ct)
    {
        return await CatalogOrderTransaction.Run(dbContext, async () =>
        {
            var comment = await dbContext.EdgeComments
                .SingleOrDefaultAsync(comment => comment.Id == command.CommentId
                    && comment.EdgeId == command.Id, ct)
                ?? throw new BadHttpRequestException("Комментарий кромки не найден.");

            historyWriter.Add(CatalogHistoryActionTypeEnum.Update, CatalogHistoryEntityTypeEnum.Edge, command.Id,
                $"Кромка #{command.Id}: удалён комментарий{(comment.IsForClient ? " для клиента" : string.Empty)} «{comment.Text}».");
            dbContext.EdgeComments.Remove(comment);
            await dbContext.SaveChangesAsync(ct);
            return new DeleteEdgeCommentCommandResult(true);
        }, ct);
    }
}