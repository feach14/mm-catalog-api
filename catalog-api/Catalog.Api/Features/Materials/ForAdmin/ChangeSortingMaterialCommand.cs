using Catalog.Api.Enums;
using Catalog.Database;
using Core.CQRS;

namespace Catalog.Api.Features.Materials.ForAdmin;

public record ChangeSortingMaterialCommand(
    [property: Description("Id материала")] int Id,
    [property: Description("Направление: (выше (+1) / ниже (-1) текущего положения"), JsonConverter(typeof(JsonStringEnumConverter))] DirectionSortEnum Direction)
    : ICommand<ChangeSortingMaterialCommandResult>;

public record ChangeSortingMaterialCommandResult(
    [property: Description("Успех операции")] bool Success);

public class ChangeSortingMaterialCommandHandler(CatalogDbContext dbContext) : ICommandHandler<ChangeSortingMaterialCommand, ChangeSortingMaterialCommandResult>
{
    public async Task<ChangeSortingMaterialCommandResult> Handle(ChangeSortingMaterialCommand command, CancellationToken ct)
    {
        var materialEntity = await  dbContext.Materials
            .Include(x => x.Category)
            .FirstAsync(x => x.Id == command.Id, ct);

        var materials = await dbContext.Materials
            .Where(x => x.CategoryId == materialEntity.CategoryId && !x.Deleted)
            .OrderBy(x => x.OrderByCol)
            .ToArrayAsync(ct);

        if (command.Direction == DirectionSortEnum.UP)
        {
            for (int i = 0; i < materials.Length; i++)
            {                    
                if (materials[i].Id == command.Id)
                {
                    if (i == 0)
                        throw new BadHttpRequestException($"Первый элемент списка в коллекции '{materialEntity.Category.Name}'. Выше некуда.");

                    (materials[i - 1].OrderByCol, materials[i].OrderByCol) = 
                        (materials[i].OrderByCol, materials[i - 1].OrderByCol);
                    break;
                }
            }
        }
        if (command.Direction == DirectionSortEnum.DOWN)
        {
            for (var i = 0; i < materials.Length; i++)
            {                    
                if (materials[i].Id == command.Id)
                {
                    if (i == materials.Length - 1)
                        throw new BadHttpRequestException($"Последний элемент списка в коллекции '{materialEntity.Category.Name}'. Ниже некуда.");

                    (materials[i + 1].OrderByCol, materials[i].OrderByCol) = 
                        (materials[i].OrderByCol, materials[i + 1].OrderByCol);
                    break;
                }
            }
        }

        foreach (var material in materials)
        {
            dbContext.Materials.Update(material);
        }
        await dbContext.SaveChangesAsync(ct);

        return new ChangeSortingMaterialCommandResult(true);
    }
}