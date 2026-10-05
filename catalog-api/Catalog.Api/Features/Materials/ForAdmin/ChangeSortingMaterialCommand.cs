using Catalog.Database;
using Core.BaseEnums;
using Core.CQRS;

namespace Catalog.Api.Features.Materials.ForAdmin;

public record ChangeSortingMaterialCommand(
    [property: Description("Id материала")] int Id,
    [property: Description("Направление: (выше (+1) / ниже (-1) текущего положения"), JsonConverter(typeof(JsonStringEnumConverter))] DirectionSortEnum Direction)
    : ICommand<ChangeSortingMaterialCommandResult>;

public sealed class ChangeSortingMaterialCommandValidator : AbstractValidator<ChangeSortingMaterialCommand>
{
    public ChangeSortingMaterialCommandValidator()
    {
        RuleFor(x => x.Id).GreaterThan(0).WithMessage("Id материала должен быть больше нуля");
        RuleFor(x => x.Direction).IsInEnum().WithMessage("Указано недопустимое направление сортировки");
    }
}

public record ChangeSortingMaterialCommandResult(
    [property: Description("Успех операции")] bool Success);

public class ChangeSortingMaterialCommandHandler(CatalogDbContext dbContext) : ICommandHandler<ChangeSortingMaterialCommand, ChangeSortingMaterialCommandResult>
{
    public async Task<ChangeSortingMaterialCommandResult> Handle(ChangeSortingMaterialCommand command, CancellationToken ct)
    {
        var materialEntity = await dbContext.Materials
            .Include(x => x.Category)
            .FirstOrDefaultAsync(x => x.Id == command.Id, ct)
            ?? throw new BadHttpRequestException($"Материал с id={command.Id} не найден.");

        var materials = await dbContext.Materials
            .Where(x => x.CategoryId == materialEntity.CategoryId)
            .OrderBy(x => x.OrderByCol)
            .ToArrayAsync(ct);

        var materialIndex = Array.FindIndex(materials, x => x.Id == command.Id);
        if (materialIndex < 0)
            throw new BadHttpRequestException($"Материал с id={command.Id} не найден.");

        var adjacentMaterialIndex = command.Direction switch
        {
            DirectionSortEnum.UP when materialIndex == 0 => throw new BadHttpRequestException($"Первый элемент списка в коллекции '{materialEntity.Category.Name}'. Выше некуда."),
            DirectionSortEnum.DOWN when materialIndex == materials.Length - 1 => throw new BadHttpRequestException($"Последний элемент списка в коллекции '{materialEntity.Category.Name}'. Ниже некуда."),
            DirectionSortEnum.UP => materialIndex - 1,
            DirectionSortEnum.DOWN => materialIndex + 1,
            _ => throw new BadHttpRequestException("Недопустимое направление сортировки.")
        };

        var material = materials[materialIndex];
        var adjacentMaterial = materials[adjacentMaterialIndex];
        var materialOrderByCol = material.OrderByCol;
        var adjacentMaterialOrderByCol = adjacentMaterial.OrderByCol;

        await using var transaction = await dbContext.Database.BeginTransactionAsync(ct);

        material.OrderByCol = int.MinValue + material.Id;
        await dbContext.SaveChangesAsync(ct);

        adjacentMaterial.OrderByCol = materialOrderByCol;
        material.OrderByCol = adjacentMaterialOrderByCol;
        await dbContext.SaveChangesAsync(ct);

        await transaction.CommitAsync(ct);

        return new ChangeSortingMaterialCommandResult(true);
    }
}