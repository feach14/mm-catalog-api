using Catalog.Api.Infrastructure.Ordering;
using Catalog.Api.Services.History;
using Catalog.Database;
using Catalog.Database.Enums;
using Core.BaseEnums;
using Core.CQRS;

namespace Catalog.Api.Features.Edges.Sizes;

public sealed record ChangeSortingEdgeSizeCommand(
    [property: Description("Id")] int Id,
    [property: Description("Направление: UP — выше, DOWN — ниже"), JsonConverter(typeof(StrictStringEnumConverter))] DirectionSortEnum Direction)
    : ICommand<ChangeSortingEdgeSizeCommandResult>;

public sealed class ChangeSortingEdgeSizeCommandValidator : AbstractValidator<ChangeSortingEdgeSizeCommand>
{
    public ChangeSortingEdgeSizeCommandValidator(CatalogDbContext dbContext)
    {
        RuleFor(x => x.Id).Cascade(CascadeMode.Stop)
            .GreaterThan(0).WithMessage("Id размера кромок должен быть больше нуля.")
            .MustAsync(async (id, ct) => await dbContext.EdgeSizes.AsNoTracking().AnyAsync(x => x.Id == id, ct)).WithMessage("Размер кромок не найден.");
        RuleFor(x => x.Direction).IsInEnum().WithMessage("Указано недопустимое направление сортировки.");
    }
}

public sealed record ChangeSortingEdgeSizeCommandResult([property: Description("Успех операции")] bool Success);

public sealed class ChangeSortingEdgeSizeCommandHandler(CatalogDbContext dbContext, ICatalogHistoryWriter historyWriter)
    : ICommandHandler<ChangeSortingEdgeSizeCommand, ChangeSortingEdgeSizeCommandResult>
{
    public async Task<ChangeSortingEdgeSizeCommandResult> Handle(ChangeSortingEdgeSizeCommand command, CancellationToken ct)
    {
        return await CatalogOrderTransaction.Run(dbContext, async () =>
        {
            var sizes = await dbContext.EdgeSizes
                .OrderBy(x => x.OrderByCol).ToArrayAsync(ct);
            var size = sizes.Single(x => x.Id == command.Id);
            var index = Array.FindIndex(sizes, x => x.Id == size.Id);
            var adjacentIndex = command.Direction switch
            {
                DirectionSortEnum.UP when index == 0 => throw new BadHttpRequestException("Первый размер в списке. Выше некуда."),
                DirectionSortEnum.DOWN when index == sizes.Length - 1 => throw new BadHttpRequestException("Последний размер в списке. Ниже некуда."),
                DirectionSortEnum.UP => index - 1,
                DirectionSortEnum.DOWN => index + 1,
                _ => throw new BadHttpRequestException("Недопустимое направление сортировки.")
            };
            var adjacentSize = sizes[adjacentIndex];
            (sizes[index], sizes[adjacentIndex]) = (sizes[adjacentIndex], sizes[index]);
            // Normalize positions for rows added before the ordering migration.
            for (var position = 0; position < sizes.Length; position++)
                sizes[position].OrderByCol = position + 1;
            historyWriter.Add(CatalogHistoryActionTypeEnum.Update, CatalogHistoryEntityTypeEnum.EdgeSize, size.Id,
                $"Порядок размера кромок #{size.Id} «{size.Name}» изменён: позиция {index + 1} → {adjacentIndex + 1}, соседний размер #{adjacentSize.Id} «{adjacentSize.Name}».");
            await dbContext.SaveChangesAsync(ct);
            return new ChangeSortingEdgeSizeCommandResult(true);
        }, ct);
    }
}