using Catalog.Database;
using Core.BaseEnums;
using Core.CQRS;

namespace Catalog.Api.Features.MaterialTypes.ForAdmin;

public sealed record ChangeSortingMaterialTypeCommand(
    [property: Description("Id типа материала")] int Id,
    [property: Description("Направление: выше или ниже текущего положения"), JsonConverter(typeof(JsonStringEnumConverter))] DirectionSortEnum Direction) : ICommand<ChangeSortingMaterialTypeCommandResult>;

public sealed class ChangeSortingMaterialTypeCommandValidator : AbstractValidator<ChangeSortingMaterialTypeCommand>
{
    public ChangeSortingMaterialTypeCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("Id типа материала должен быть больше нуля");
        RuleFor(x => x.Direction).IsInEnum().WithMessage("Указано недопустимое направление сортировки");
    }
}

public sealed record ChangeSortingMaterialTypeCommandResult(
    [property: Description("Успех операции")] bool Success);

public sealed class ChangeSortingMaterialTypeCommandHandler(CatalogDbContext dbContext)
    : ICommandHandler<ChangeSortingMaterialTypeCommand, ChangeSortingMaterialTypeCommandResult>
{
    public async Task<ChangeSortingMaterialTypeCommandResult> Handle(ChangeSortingMaterialTypeCommand command, CancellationToken ct)
    {
        var materialTypes = await dbContext.MaterialTypes.OrderBy(x => x.OrderByCol).ToArrayAsync(ct);
        var index = Array.FindIndex(materialTypes, x => x.Id == command.Id);
        if (index < 0)
            throw new BadHttpRequestException($"Тип материала с id={command.Id} не найден.");

        var adjacentIndex = command.Direction switch
        {
            DirectionSortEnum.UP when index == 0 => throw new BadHttpRequestException("Первый элемент в списке. Выше некуда."),
            DirectionSortEnum.DOWN when index == materialTypes.Length - 1 => throw new BadHttpRequestException("Последний элемент в списке. Ниже некуда."),
            DirectionSortEnum.UP => index - 1,
            DirectionSortEnum.DOWN => index + 1,
            _ => throw new BadHttpRequestException("Недопустимое направление сортировки.")
        };

        var materialType = materialTypes[index];
        var adjacent = materialTypes[adjacentIndex];
        var order = materialType.OrderByCol;
        var adjacentOrder = adjacent.OrderByCol;
        await using var transaction = await dbContext.Database.BeginTransactionAsync(ct);
        materialType.OrderByCol = int.MinValue + materialType.Id;
        await dbContext.SaveChangesAsync(ct);
        adjacent.OrderByCol = order;
        materialType.OrderByCol = adjacentOrder;
        await dbContext.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return new ChangeSortingMaterialTypeCommandResult(true);
    }
}