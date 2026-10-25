using Catalog.Api.Infrastructure.Ordering;
using Catalog.Api.Services.History;
using Catalog.Database;
using Catalog.Database.Enums;
using Core.BaseEnums;
using Core.CQRS;

namespace Catalog.Api.Features.Relations.Markings;

public sealed record ChangeSortingRelationMarkingCommand(
    [property: Description("Id")] int Id,
    [property: Description("Направление: UP — выше, DOWN — ниже"), JsonConverter(typeof(StrictStringEnumConverter))] DirectionSortEnum Direction)
    : ICommand<ChangeSortingRelationMarkingCommandResult>;

public sealed class ChangeSortingRelationMarkingCommandValidator : AbstractValidator<ChangeSortingRelationMarkingCommand>
{
    public ChangeSortingRelationMarkingCommandValidator(CatalogDbContext dbContext)
    {
        RuleFor(x => x.Id).Cascade(CascadeMode.Stop)
            .GreaterThan(0).WithMessage("Id маркировки должен быть больше нуля.")
            .MustAsync(async (id, ct) => await dbContext.RelationMarkings.AsNoTracking().AnyAsync(x => x.Id == id, ct)).WithMessage("Указанная маркировка не найдена.");
        RuleFor(x => x.Direction).IsInEnum().WithMessage("Указано недопустимое направление сортировки.");
    }
}

public sealed record ChangeSortingRelationMarkingCommandResult([property: Description("Успех операции")] bool Success);

public sealed class ChangeSortingRelationMarkingCommandHandler(CatalogDbContext dbContext, ICatalogHistoryWriter historyWriter)
    : ICommandHandler<ChangeSortingRelationMarkingCommand, ChangeSortingRelationMarkingCommandResult>
{
    public async Task<ChangeSortingRelationMarkingCommandResult> Handle(ChangeSortingRelationMarkingCommand command, CancellationToken ct)
    {
        return await CatalogOrderTransaction.Run(dbContext, async () =>
        {
            var markings = await dbContext.RelationMarkings.OrderBy(x => x.OrderByCol).ThenBy(x => x.Id).ToArrayAsync(ct);
            var marking = markings.Single(x => x.Id == command.Id);
            var index = Array.FindIndex(markings, x => x.Id == marking.Id);
            var adjacentIndex = command.Direction switch
            {
                DirectionSortEnum.UP when index == 0 => throw new BadHttpRequestException("Первая маркировка в списке. Выше некуда."),
                DirectionSortEnum.DOWN when index == markings.Length - 1 => throw new BadHttpRequestException("Последняя маркировка в списке. Ниже некуда."),
                DirectionSortEnum.UP => index - 1,
                DirectionSortEnum.DOWN => index + 1,
                _ => throw new BadHttpRequestException("Недопустимое направление сортировки.")
            };
            var adjacentMarking = markings[adjacentIndex];
            (markings[index], markings[adjacentIndex]) = (markings[adjacentIndex], markings[index]);
            // Existing rows start at zero after migration; persist their relative order with the requested move.
            for (var position = 0; position < markings.Length; position++)
                markings[position].OrderByCol = position + 1;
            historyWriter.Add(CatalogHistoryActionTypeEnum.Update, CatalogHistoryEntityTypeEnum.RelationMarking, marking.Id,
                $"Порядок маркировки #{marking.Id} «{marking.Name}» изменён: позиция {index + 1} → {adjacentIndex + 1}, соседняя маркировка #{adjacentMarking.Id} «{adjacentMarking.Name}».");
            await dbContext.SaveChangesAsync(ct);
            return new ChangeSortingRelationMarkingCommandResult(true);
        }, ct);
    }
}