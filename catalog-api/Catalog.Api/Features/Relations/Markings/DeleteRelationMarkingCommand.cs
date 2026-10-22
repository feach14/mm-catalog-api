using Catalog.Api.Services.History;
using Catalog.Database;
using Catalog.Database.Entities;
using Catalog.Database.Enums;
using Core.CQRS;

namespace Catalog.Api.Features.Relations.Markings;

public sealed record DeleteRelationMarkingCommand(int Id) : ICommand<DeleteRelationMarkingCommandResult>;

public sealed record DeleteRelationMarkingCommandResult(
    [property: Description("Успех операции")] bool Success);

public sealed class DeleteRelationMarkingCommandHandler(CatalogDbContext dbContext, ICatalogHistoryWriter historyWriter)
    : ICommandHandler<DeleteRelationMarkingCommand, DeleteRelationMarkingCommandResult>
{
    public async Task<DeleteRelationMarkingCommandResult> Handle(DeleteRelationMarkingCommand command, CancellationToken ct)
    {
        RelationMarking marking = await dbContext.RelationMarkings.SingleOrDefaultAsync(x => x.Id == command.Id, ct)
            ?? throw new BadHttpRequestException("Маркировка не найдена.");
        if (await dbContext.MaterialEdgeLinkMarkings.AsNoTracking().AnyAsync(x => x.MarkingId == command.Id, ct))
            throw new BadHttpRequestException("Нельзя удалить маркировку, которая используется в связях материалов с кромками.");

        dbContext.RelationMarkings.Remove(marking);
        historyWriter.Add(
            CatalogHistoryActionTypeEnum.Delete,
            CatalogHistoryEntityTypeEnum.RelationMarking,
            marking.Id,
            $"Удалена маркировка #{marking.Id} «{marking.Name}».");
        await dbContext.SaveChangesAsync(ct);
        return new DeleteRelationMarkingCommandResult(true);
    }
}