using Catalog.Api.Infrastructure.Ordering;
using Catalog.Api.Services.History;
using Catalog.Database;
using Catalog.Database.Enums;
using Core.BaseEnums;
using Core.CQRS;

namespace Catalog.Api.Features.Edges.SaleUnits;

public sealed record ChangeSortingEdgeSaleUnitCommand(
    [property: Description("Id")] int Id,
    [property: Description("Направление: UP — выше, DOWN — ниже"), JsonConverter(typeof(StrictStringEnumConverter))] DirectionSortEnum Direction)
    : ICommand<ChangeSortingEdgeSaleUnitCommandResult>;

public sealed class ChangeSortingEdgeSaleUnitCommandValidator : AbstractValidator<ChangeSortingEdgeSaleUnitCommand>
{
    public ChangeSortingEdgeSaleUnitCommandValidator(CatalogDbContext dbContext)
    {
        RuleFor(x => x.Id).Cascade(CascadeMode.Stop)
            .GreaterThan(0).WithMessage("Id единицы продажи кромок должен быть больше нуля.")
            .MustAsync(async (id, ct) => await dbContext.EdgeSaleUnits.AsNoTracking().AnyAsync(x => x.Id == id, ct)).WithMessage("Единица продажи кромок не найдена.");
        RuleFor(x => x.Direction).IsInEnum().WithMessage("Указано недопустимое направление сортировки.");
    }
}

public sealed record ChangeSortingEdgeSaleUnitCommandResult([property: Description("Успех операции")] bool Success);

public sealed class ChangeSortingEdgeSaleUnitCommandHandler(CatalogDbContext dbContext, ICatalogHistoryWriter historyWriter)
    : ICommandHandler<ChangeSortingEdgeSaleUnitCommand, ChangeSortingEdgeSaleUnitCommandResult>
{
    public async Task<ChangeSortingEdgeSaleUnitCommandResult> Handle(ChangeSortingEdgeSaleUnitCommand command, CancellationToken ct)
    {
        return await CatalogOrderTransaction.Run(dbContext, async () =>
        {
            var saleUnits = await dbContext.EdgeSaleUnits
                .OrderBy(x => x.OrderByCol).ToArrayAsync(ct);
            var saleUnit = saleUnits.Single(x => x.Id == command.Id);
            var index = Array.FindIndex(saleUnits, x => x.Id == saleUnit.Id);
            var adjacentIndex = command.Direction switch
            {
                DirectionSortEnum.UP when index == 0 => throw new BadHttpRequestException("Первая единица продажи в списке. Выше некуда."),
                DirectionSortEnum.DOWN when index == saleUnits.Length - 1 => throw new BadHttpRequestException("Последняя единица продажи в списке. Ниже некуда."),
                DirectionSortEnum.UP => index - 1,
                DirectionSortEnum.DOWN => index + 1,
                _ => throw new BadHttpRequestException("Недопустимое направление сортировки.")
            };
            var adjacentSaleUnit = saleUnits[adjacentIndex];
            (saleUnits[index], saleUnits[adjacentIndex]) = (saleUnits[adjacentIndex], saleUnits[index]);
            // Normalize positions for rows added before the ordering migration.
            for (var position = 0; position < saleUnits.Length; position++)
                saleUnits[position].OrderByCol = position + 1;
            historyWriter.Add(CatalogHistoryActionTypeEnum.Update, CatalogHistoryEntityTypeEnum.EdgeSaleUnit, saleUnit.Id,
                $"Порядок единицы продажи кромок #{saleUnit.Id} «{saleUnit.Name}» изменён: позиция {index + 1} → {adjacentIndex + 1}, соседняя единица продажи #{adjacentSaleUnit.Id} «{adjacentSaleUnit.Name}».");
            await dbContext.SaveChangesAsync(ct);
            return new ChangeSortingEdgeSaleUnitCommandResult(true);
        }, ct);
    }
}