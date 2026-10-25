using Catalog.Api.Infrastructure.Ordering;
using Catalog.Api.Services.History;
using Catalog.Database;
using Catalog.Database.Enums;
using Core.BaseEnums;
using Core.CQRS;

namespace Catalog.Api.Features.Edges.ForAdmin;

public sealed record ChangeSortingEdgeCommand(
    [property: Description("Id кромки")] int Id,
    [property: Description("Направление: UP — выше, DOWN — ниже"), JsonConverter(typeof(StrictStringEnumConverter))] DirectionSortEnum Direction)
    : ICommand<ChangeSortingEdgeCommandResult>;

public sealed class ChangeSortingEdgeCommandValidator : AbstractValidator<ChangeSortingEdgeCommand>
{
    public ChangeSortingEdgeCommandValidator(CatalogDbContext dbContext)
    {
        RuleFor(x => x.Id).Cascade(CascadeMode.Stop)
            .GreaterThan(0).WithMessage("Id кромки должен быть больше нуля.")
            .MustAsync(async (id, ct) => await dbContext.Edges.AsNoTracking().AnyAsync(x => x.Id == id, ct)).WithMessage("Указанная кромка не найдена.");
        RuleFor(x => x.Direction).IsInEnum().WithMessage("Указано недопустимое направление сортировки.");
    }
}

public sealed record ChangeSortingEdgeCommandResult([property: Description("Успех операции")] bool Success);

public sealed class ChangeSortingEdgeCommandHandler(CatalogDbContext dbContext, ICatalogHistoryWriter historyWriter)
    : ICommandHandler<ChangeSortingEdgeCommand, ChangeSortingEdgeCommandResult>
{
    public async Task<ChangeSortingEdgeCommandResult> Handle(ChangeSortingEdgeCommand command, CancellationToken ct)
    {
        return await CatalogOrderTransaction.Run(dbContext, async () =>
        {
            var edges = await dbContext.Edges.OrderBy(x => x.OrderByCol).ThenBy(x => x.Id).ToArrayAsync(ct);
            var edge = edges.Single(x => x.Id == command.Id);
            var index = Array.FindIndex(edges, x => x.Id == edge.Id);
            var adjacentIndex = command.Direction switch
            {
                DirectionSortEnum.UP when index == 0 => throw new BadHttpRequestException("Первая кромка в списке. Выше некуда."),
                DirectionSortEnum.DOWN when index == edges.Length - 1 => throw new BadHttpRequestException("Последняя кромка в списке. Ниже некуда."),
                DirectionSortEnum.UP => index - 1,
                DirectionSortEnum.DOWN => index + 1,
                _ => throw new BadHttpRequestException("Недопустимое направление сортировки.")
            };
            var adjacentEdge = edges[adjacentIndex];
            (edges[index], edges[adjacentIndex]) = (edges[adjacentIndex], edges[index]);
            // Existing rows start at zero after migration; persist their current relative order with the requested move.
            for (var position = 0; position < edges.Length; position++)
                edges[position].OrderByCol = position + 1;
            historyWriter.Add(CatalogHistoryActionTypeEnum.Update, CatalogHistoryEntityTypeEnum.Edge, edge.Id,
                $"Порядок кромки #{edge.Id} «{edge.Name}» изменён: позиция {index + 1} → {adjacentIndex + 1}, соседняя кромка #{adjacentEdge.Id} «{adjacentEdge.Name}».");
            await dbContext.SaveChangesAsync(ct);
            return new ChangeSortingEdgeCommandResult(true);
        }, ct);
    }
}