using Catalog.Api.Features.Relations.Markings.Dto;
using Catalog.Api.Services.History;
using Catalog.Database;
using Catalog.Database.Entities;
using Catalog.Database.Enums;
using Core.CQRS;

namespace Catalog.Api.Features.Relations.Markings;

public sealed record UpdateRelationMarkingCommand(int Id, RelationMarkingModel Model) : ICommand<UpdateRelationMarkingCommandResult>;

public sealed record UpdateRelationMarkingCommandResult(
    [property: Description("Успех операции")] bool Success);

public sealed class UpdateRelationMarkingCommandHandler(CatalogDbContext dbContext, ICatalogHistoryWriter historyWriter)
    : ICommandHandler<UpdateRelationMarkingCommand, UpdateRelationMarkingCommandResult>
{
    public async Task<UpdateRelationMarkingCommandResult> Handle(UpdateRelationMarkingCommand command, CancellationToken ct)
    {
        RelationMarking marking = await dbContext.RelationMarkings.SingleOrDefaultAsync(x => x.Id == command.Id, ct)
            ?? throw new BadHttpRequestException("Маркировка не найдена.");

        string name = command.Model.Name.Trim();
        List<string> changes = [];
        if (marking.Name != name)
            changes.Add($"название «{marking.Name}» → «{name}»");

        if (changes.Count == 0)
            return new UpdateRelationMarkingCommandResult(true);

        marking.Name = name;
        historyWriter.Add(
            CatalogHistoryActionTypeEnum.Update,
            CatalogHistoryEntityTypeEnum.RelationMarking,
            marking.Id,
            $"Маркировка #{marking.Id}: {string.Join(", ", changes)}.");
        await dbContext.SaveChangesAsync(ct);
        return new UpdateRelationMarkingCommandResult(true);
    }
}