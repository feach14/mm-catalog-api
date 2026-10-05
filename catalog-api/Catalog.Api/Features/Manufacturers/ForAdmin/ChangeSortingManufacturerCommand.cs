using Catalog.Database;
using Core.BaseEnums;
using Core.CQRS;

namespace Catalog.Api.Features.Manufacturers.ForAdmin;

public sealed record ChangeSortingManufacturerCommand(
    [property: Description("Id производителя")] int Id,
    [property: Description("Направление: выше или ниже текущего положения"), JsonConverter(typeof(JsonStringEnumConverter))] DirectionSortEnum Direction) : ICommand<ChangeSortingManufacturerCommandResult>;

public sealed class ChangeSortingManufacturerCommandValidator : AbstractValidator<ChangeSortingManufacturerCommand>
{
    public ChangeSortingManufacturerCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("Id производителя должен быть больше нуля");
        RuleFor(x => x.Direction).IsInEnum().WithMessage("Указано недопустимое направление сортировки");
    }
}

public sealed record ChangeSortingManufacturerCommandResult(
    [property: Description("Успех операции")] bool Success);

public sealed class ChangeSortingManufacturerCommandHandler(CatalogDbContext dbContext)
    : ICommandHandler<ChangeSortingManufacturerCommand, ChangeSortingManufacturerCommandResult>
{
    public async Task<ChangeSortingManufacturerCommandResult> Handle(ChangeSortingManufacturerCommand command, CancellationToken ct)
    {
        var manufacturers = await dbContext.MaterialManufacturers.OrderBy(x => x.OrderByCol).ToArrayAsync(ct);
        var index = Array.FindIndex(manufacturers, x => x.Id == command.Id);
        if (index < 0)
            throw new BadHttpRequestException($"Производитель с id={command.Id} не найден.");

        var adjacentIndex = command.Direction switch
        {
            DirectionSortEnum.UP when index == 0 => throw new BadHttpRequestException("Первый элемент в списке. Выше некуда."),
            DirectionSortEnum.DOWN when index == manufacturers.Length - 1 => throw new BadHttpRequestException("Последний элемент в списке. Ниже некуда."),
            DirectionSortEnum.UP => index - 1,
            DirectionSortEnum.DOWN => index + 1,
            _ => throw new BadHttpRequestException("Недопустимое направление сортировки.")
        };

        var manufacturer = manufacturers[index];
        var adjacent = manufacturers[adjacentIndex];
        var order = manufacturer.OrderByCol;
        var adjacentOrder = adjacent.OrderByCol;
        await using var transaction = await dbContext.Database.BeginTransactionAsync(ct);
        manufacturer.OrderByCol = int.MinValue + manufacturer.Id;
        await dbContext.SaveChangesAsync(ct);
        adjacent.OrderByCol = order;
        manufacturer.OrderByCol = adjacentOrder;
        await dbContext.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return new ChangeSortingManufacturerCommandResult(true);
    }
}