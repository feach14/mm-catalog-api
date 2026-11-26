using Catalog.Api.Infrastructure.Ordering;
using Catalog.Api.Services.History;
using Catalog.Database;
using Catalog.Database.Enums;
using Core.BaseEnums;
using Core.CQRS;

namespace Catalog.Api.Features.Edges.Suppliers;

public sealed record ChangeSortingEdgeSupplierCommand(
    [property: Description("Id")] int Id,
    [property: Description("Направление: UP — выше, DOWN — ниже"), JsonConverter(typeof(StrictStringEnumConverter))] DirectionSortEnum Direction)
    : ICommand<ChangeSortingEdgeSupplierCommandResult>;

public sealed class ChangeSortingEdgeSupplierCommandValidator : AbstractValidator<ChangeSortingEdgeSupplierCommand>
{
    public ChangeSortingEdgeSupplierCommandValidator(CatalogDbContext dbContext)
    {
        RuleFor(x => x.Id).Cascade(CascadeMode.Stop)
            .GreaterThan(0).WithMessage("Id поставщика кромок должен быть больше нуля.")
            .MustAsync(async (id, ct) => await dbContext.EdgeSuppliers.AsNoTracking().AnyAsync(x => x.Id == id, ct)).WithMessage("Поставщик кромок не найден.");
        RuleFor(x => x.Direction).IsInEnum().WithMessage("Указано недопустимое направление сортировки.");
    }
}

public sealed record ChangeSortingEdgeSupplierCommandResult([property: Description("Успех операции")] bool Success);

public sealed class ChangeSortingEdgeSupplierCommandHandler(CatalogDbContext dbContext, ICatalogHistoryWriter historyWriter)
    : ICommandHandler<ChangeSortingEdgeSupplierCommand, ChangeSortingEdgeSupplierCommandResult>
{
    public async Task<ChangeSortingEdgeSupplierCommandResult> Handle(ChangeSortingEdgeSupplierCommand command, CancellationToken ct)
    {
        return await CatalogOrderTransaction.Run(dbContext, async () =>
        {
            var suppliers = await dbContext.EdgeSuppliers
                .OrderBy(x => x.OrderByCol).ToArrayAsync(ct);
            var supplier = suppliers.Single(x => x.Id == command.Id);
            var index = Array.FindIndex(suppliers, x => x.Id == supplier.Id);
            var adjacentIndex = command.Direction switch
            {
                DirectionSortEnum.UP when index == 0 => throw new BadHttpRequestException("Первый поставщик в списке. Выше некуда."),
                DirectionSortEnum.DOWN when index == suppliers.Length - 1 => throw new BadHttpRequestException("Последний поставщик в списке. Ниже некуда."),
                DirectionSortEnum.UP => index - 1,
                DirectionSortEnum.DOWN => index + 1,
                _ => throw new BadHttpRequestException("Недопустимое направление сортировки.")
            };
            var adjacentSupplier = suppliers[adjacentIndex];
            (suppliers[index], suppliers[adjacentIndex]) = (suppliers[adjacentIndex], suppliers[index]);
            for (var position = 0; position < suppliers.Length; position++)
                suppliers[position].OrderByCol = position + 1;
            historyWriter.Add(CatalogHistoryActionTypeEnum.Update, CatalogHistoryEntityTypeEnum.EdgeSupplier, supplier.Id,
                $"Порядок поставщика кромок #{supplier.Id} «{supplier.Name}» изменён: позиция {index + 1} → {adjacentIndex + 1}, соседний поставщик #{adjacentSupplier.Id} «{adjacentSupplier.Name}».");
            await dbContext.SaveChangesAsync(ct);
            return new ChangeSortingEdgeSupplierCommandResult(true);
        }, ct);
    }
}