using Catalog.Database;
using Core.CQRS;

namespace Catalog.Api.Features.History;

public sealed record DeleteCatalogTestRunHistoryCommand(int RunId)
    : ICommand<DeleteCatalogTestRunHistoryCommandResult>;

public sealed record DeleteCatalogTestRunHistoryCommandResult(
    [property: Description("Количество удалённых записей истории")] int DeletedCount);

public sealed class DeleteCatalogTestRunHistoryCommandHandler(CatalogDbContext dbContext)
    : ICommandHandler<DeleteCatalogTestRunHistoryCommand, DeleteCatalogTestRunHistoryCommandResult>
{
    public async Task<DeleteCatalogTestRunHistoryCommandResult> Handle(
        DeleteCatalogTestRunHistoryCommand command,
        CancellationToken ct)
    {
        var pattern = $"%codex-history-{command.RunId}%";
        var deletedCount = await dbContext.ChangeHistory
            .Where(x => EF.Functions.ILike(x.Message, pattern))
            .ExecuteDeleteAsync(ct);

        return new DeleteCatalogTestRunHistoryCommandResult(deletedCount);
    }
}