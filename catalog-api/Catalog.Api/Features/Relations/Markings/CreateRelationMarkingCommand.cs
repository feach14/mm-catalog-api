using Catalog.Api.Features.Relations.Markings.Dto;
using Catalog.Api.Services.History;
using Catalog.Database;
using Catalog.Database.Entities;
using Catalog.Database.Enums;
using Core.CQRS;

namespace Catalog.Api.Features.Relations.Markings;

public sealed record CreateRelationMarkingCommand(RelationMarkingModel Model) : ICommand<CreateRelationMarkingCommandResult>;

public sealed record CreateRelationMarkingCommandResult(
    [property: Description("Id")] int Id);

public sealed class CreateRelationMarkingCommandHandler(CatalogDbContext dbContext, ICatalogHistoryWriter historyWriter)
    : ICommandHandler<CreateRelationMarkingCommand, CreateRelationMarkingCommandResult>
{
    public async Task<CreateRelationMarkingCommandResult> Handle(CreateRelationMarkingCommand command, CancellationToken ct)
    {
        RelationMarking marking = new()
        {
            Name = command.Model.Name.Trim()
        };

        await using var transaction = await dbContext.Database.BeginTransactionAsync(ct);
        dbContext.RelationMarkings.Add(marking);
        await dbContext.SaveChangesAsync(ct);
        historyWriter.Add(
            CatalogHistoryActionTypeEnum.Create,
            CatalogHistoryEntityTypeEnum.RelationMarking,
            marking.Id,
            $"Создана маркировка #{marking.Id} «{marking.Name}».");
        await dbContext.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return new CreateRelationMarkingCommandResult(marking.Id);
    }
}