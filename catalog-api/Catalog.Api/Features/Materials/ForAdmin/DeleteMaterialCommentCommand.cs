using Catalog.Api.Infrastructure.Ordering;
using Catalog.Api.Services.History;
using Catalog.Database;
using Catalog.Database.Enums;
using Core.CQRS;

namespace Catalog.Api.Features.Materials.ForAdmin;

public sealed record DeleteMaterialCommentCommand(int Id, int CommentId) : ICommand<DeleteMaterialCommentCommandResult>;

public sealed record DeleteMaterialCommentCommandResult(
    [property: Description("Успех операции")] bool Success);

public sealed class DeleteMaterialCommentCommandHandler(CatalogDbContext dbContext, ICatalogHistoryWriter historyWriter)
    : ICommandHandler<DeleteMaterialCommentCommand, DeleteMaterialCommentCommandResult>
{
    public async Task<DeleteMaterialCommentCommandResult> Handle(DeleteMaterialCommentCommand command, CancellationToken ct)
    {
        return await CatalogOrderTransaction.Run(dbContext, async () =>
        {
            var comment = await dbContext.MaterialComments
                .SingleOrDefaultAsync(comment => comment.Id == command.CommentId
                    && (comment.MaterialId == command.Id || comment.ArchivedMaterialId == command.Id), ct)
                ?? throw new BadHttpRequestException("Комментарий материала не найден.");

            historyWriter.Add(CatalogHistoryActionTypeEnum.Update, CatalogHistoryEntityTypeEnum.Material, command.Id,
                $"Материал #{command.Id}: удалён комментарий «{comment.Text}».");
            dbContext.MaterialComments.Remove(comment);
            await dbContext.SaveChangesAsync(ct);
            return new DeleteMaterialCommentCommandResult(true);
        }, ct);
    }
}