using Catalog.Api.Enums;
using Catalog.Database;
using Core.CQRS;

namespace Catalog.Api.Features.MaterialCategories.ForAdmin;

public record ChangeSortingMaterialCategoryCommand(
    [property: Description("Id категории")] int CategoryId,
    [property: Description("Направление: (выше (+1) / ниже (-1) текущего положения"), JsonConverter(typeof(JsonStringEnumConverter))] DirectionSortEnum Direction)
    : ICommand<ChangeSortingMaterialCategoryCommandResult>;

public record ChangeSortingMaterialCategoryCommandResult(
    [property: Description("Успех операции")] bool Success);

public class ChangeSortingMaterialCategoryCommandHandler(CatalogDbContext dbContext) : ICommandHandler<ChangeSortingMaterialCategoryCommand, ChangeSortingMaterialCategoryCommandResult>
{
    public async Task<ChangeSortingMaterialCategoryCommandResult> Handle(ChangeSortingMaterialCategoryCommand command, CancellationToken ct)
    {
        var categoryMaterials = await dbContext.MaterialCategories
            .OrderBy(x => x.OrderByCol)
            .ToArrayAsync(ct);

        if (command.Direction == DirectionSortEnum.UP)
        {
            for (int i = 0; i < categoryMaterials.Length; i++)
            {                    
                if (categoryMaterials[i].Id == command.CategoryId)
                {
                    if (i == 0)
                        throw new BadHttpRequestException("Первый элемент в списке. Выше некуда.");

                    (categoryMaterials[i - 1].OrderByCol, categoryMaterials[i].OrderByCol) = 
                        (categoryMaterials[i].OrderByCol, categoryMaterials[i - 1].OrderByCol);
                    break;
                }
            }
        }
        if (command.Direction == DirectionSortEnum.DOWN)
        {
            for (var i = 0; i < categoryMaterials.Length; i++)
            {                    
                if (categoryMaterials[i].Id == command.CategoryId)
                {
                    if (i == categoryMaterials.Length - 1)
                        throw new BadHttpRequestException("Последний элемент в списке. Ниже некуда.");

                    (categoryMaterials[i + 1].OrderByCol, categoryMaterials[i].OrderByCol) = 
                        (categoryMaterials[i].OrderByCol, categoryMaterials[i + 1].OrderByCol);
                    break;
                }
            }
        }

        foreach (var category in categoryMaterials)
        {
            dbContext.MaterialCategories.Update(category);
        }
        await dbContext.SaveChangesAsync(ct);

        return new ChangeSortingMaterialCategoryCommandResult(true);
    }
}