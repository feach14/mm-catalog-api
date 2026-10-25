using Catalog.Api.Features.Relations.Markings.Dto;
using Catalog.Api.Infrastructure.Ordering;
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
        return await CatalogOrderTransaction.Run(dbContext, async () =>
        {
            var maxOrderByCol = await dbContext.RelationMarkings.MaxAsync(x => (int?)x.OrderByCol, ct) ?? 0;
            if (maxOrderByCol == int.MaxValue)
                throw new BadHttpRequestException("Достигнут предел порядка маркировок.");
            RelationMarking marking = new()
            {
                Name = command.Model.Name.Trim(),
                OrderByCol = maxOrderByCol + 1
            };

            dbContext.RelationMarkings.Add(marking);
            await dbContext.SaveChangesAsync(ct);
            historyWriter.Add(
                CatalogHistoryActionTypeEnum.Create,
                CatalogHistoryEntityTypeEnum.RelationMarking,
                marking.Id,
                $"Создана маркировка #{marking.Id} «{marking.Name}».");
            await dbContext.SaveChangesAsync(ct);
            return new CreateRelationMarkingCommandResult(marking.Id);
        }, ct);
    }
}