namespace Catalog.Api.Features.MaterialCategories.ForAdmin;

using Core.CQRS;
using Database;
using Enums;

public record ChangeSortingMaterialCategoryCommand(
    [property: Description("Id категории")] int CategoryId,
    [property: Description("Направление: (выше (+1) / ниже (-1) текущего положения"), JsonConverter(typeof(JsonStringEnumConverter))] DirectionSortEnum Direction)
    : ICommand<ChangeSortingMaterialCategoryCommandResult>;

public class ChangeSortingMaterialCategoryCommandValidator : AbstractValidator<ChangeSortingMaterialCategoryCommand>
{
    public ChangeSortingMaterialCategoryCommandValidator()
    {
        RuleFor(x => x.CategoryId).GreaterThan(0);
        RuleFor(x => x.Direction).IsInEnum();
    }
}

public record ChangeSortingMaterialCategoryCommandResult(
    [property: Description("Успех операции")] bool Success);

public class ChangeSortingMaterialCategoryCommandHandler(CatalogDbContext dbContext) : ICommandHandler<ChangeSortingMaterialCategoryCommand, ChangeSortingMaterialCategoryCommandResult>
{
    public async Task<ChangeSortingMaterialCategoryCommandResult> Handle(ChangeSortingMaterialCategoryCommand command, CancellationToken ct)
    {
        var categories = await dbContext.MaterialCategories
            .OrderBy(x => x.OrderByCol)
            .ToArrayAsync(ct);

        var categoryIndex = Array.FindIndex(categories, x => x.Id == command.CategoryId);
        if (categoryIndex < 0)
            throw new BadHttpRequestException($"Категория материалов с id={command.CategoryId} не найдена.");

        var adjacentCategoryIndex = command.Direction switch
        {
            DirectionSortEnum.UP when categoryIndex == 0 => throw new BadHttpRequestException("Первый элемент в списке. Выше некуда."),
            DirectionSortEnum.DOWN when categoryIndex == categories.Length - 1 => throw new BadHttpRequestException("Последний элемент в списке. Ниже некуда."),
            DirectionSortEnum.UP => categoryIndex - 1,
            DirectionSortEnum.DOWN => categoryIndex + 1,
            _ => throw new BadHttpRequestException("Недопустимое направление сортировки.")
        };

        var category = categories[categoryIndex];
        var adjacentCategory = categories[adjacentCategoryIndex];
        var categoryOrderByCol = category.OrderByCol;
        var adjacentCategoryOrderByCol = adjacentCategory.OrderByCol;

        await using var transaction = await dbContext.Database.BeginTransactionAsync(ct);

        category.OrderByCol = int.MinValue + category.Id;
        await dbContext.SaveChangesAsync(ct);

        adjacentCategory.OrderByCol = categoryOrderByCol;
        category.OrderByCol = adjacentCategoryOrderByCol;
        await dbContext.SaveChangesAsync(ct);

        await transaction.CommitAsync(ct);

        return new ChangeSortingMaterialCategoryCommandResult(true);
    }
}
