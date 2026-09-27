using Catalog.Api.Enums;
using Catalog.Database;
using Core.CQRS;

namespace Catalog.Api.Features.MaterialSheetSizes.ForAdmin;

public sealed record ChangeSortingMaterialSheetSizeCommand(
    [property: Description("Id размера материала")] int Id,
    [property: Description("Направление: выше или ниже текущего положения"), JsonConverter(typeof(JsonStringEnumConverter))]
    DirectionSortEnum Direction)
    : ICommand<ChangeSortingMaterialSheetSizeCommandResult>;

public sealed class ChangeSortingMaterialSheetSizeCommandValidator
    : AbstractValidator<ChangeSortingMaterialSheetSizeCommand>
{
    public ChangeSortingMaterialSheetSizeCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0);
        RuleFor(x => x.Direction).IsInEnum();
    }
}

public sealed record ChangeSortingMaterialSheetSizeCommandResult(
    [property: Description("Успех операции")] bool Success);

public sealed class ChangeSortingMaterialSheetSizeCommandHandler(CatalogDbContext dbContext)
    : ICommandHandler<ChangeSortingMaterialSheetSizeCommand, ChangeSortingMaterialSheetSizeCommandResult>
{
    public async Task<ChangeSortingMaterialSheetSizeCommandResult> Handle(
        ChangeSortingMaterialSheetSizeCommand command,
        CancellationToken ct)
    {
        var sizes = await dbContext.MaterialSheetSizes
            .OrderBy(x => x.OrderByCol)
            .ToArrayAsync(ct);

        var sizeIndex = Array.FindIndex(sizes, x => x.Id == command.Id);
        if (sizeIndex < 0)
            throw new BadHttpRequestException($"Размер материала с id={command.Id} не найден.");

        var adjacentSizeIndex = command.Direction switch
        {
            DirectionSortEnum.UP when sizeIndex == 0 =>
                throw new BadHttpRequestException("Первый элемент в списке. Выше некуда."),
            DirectionSortEnum.DOWN when sizeIndex == sizes.Length - 1 =>
                throw new BadHttpRequestException("Последний элемент в списке. Ниже некуда."),
            DirectionSortEnum.UP => sizeIndex - 1,
            DirectionSortEnum.DOWN => sizeIndex + 1,
            _ => throw new BadHttpRequestException("Недопустимое направление сортировки.")
        };

        var size = sizes[sizeIndex];
        var adjacentSize = sizes[adjacentSizeIndex];
        var sizeOrderByCol = size.OrderByCol;
        var adjacentSizeOrderByCol = adjacentSize.OrderByCol;

        await using var transaction = await dbContext.Database.BeginTransactionAsync(ct);

        size.OrderByCol = int.MinValue + size.Id;
        await dbContext.SaveChangesAsync(ct);

        adjacentSize.OrderByCol = sizeOrderByCol;
        size.OrderByCol = adjacentSizeOrderByCol;
        await dbContext.SaveChangesAsync(ct);

        await transaction.CommitAsync(ct);
        return new ChangeSortingMaterialSheetSizeCommandResult(true);
    }
}