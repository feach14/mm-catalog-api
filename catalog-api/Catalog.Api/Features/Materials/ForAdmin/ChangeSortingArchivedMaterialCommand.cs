using Catalog.Api.Infrastructure.Ordering;
using Catalog.Database;
using Core.BaseEnums;
using Core.CQRS;

namespace Catalog.Api.Features.Materials.ForAdmin;

public sealed record ChangeSortingArchivedMaterialCommand(int Id, DirectionSortEnum Direction) : ICommand<ChangeSortingMaterialCommandResult>;

public class ChangeSortingArchivedMaterialCommandHandler(CatalogDbContext dbContext) : ICommandHandler<ChangeSortingArchivedMaterialCommand, ChangeSortingMaterialCommandResult>
{
    public Task<ChangeSortingMaterialCommandResult> Handle(ChangeSortingArchivedMaterialCommand command, CancellationToken ct) =>
        dbContext.Database.CurrentTransaction is null
            ? CatalogOrderTransaction.Run(dbContext, () => Execute(command, ct), ct)
            : Execute(command, ct);

    private async Task<ChangeSortingMaterialCommandResult> Execute(ChangeSortingArchivedMaterialCommand command, CancellationToken ct)
    {
        var materials = await dbContext.ArchivedMaterials.OrderBy(x => x.OrderByCol).ThenBy(x => x.Id).ToArrayAsync(ct);
        var material = materials.SingleOrDefault(x => x.Id == command.Id)
            ?? throw new BadHttpRequestException("Материал не найден.");
        var materialIndex = Array.FindIndex(materials, x => x.Id == material.Id);

        var adjacentMaterialIndex = command.Direction switch
        {
            DirectionSortEnum.UP when materialIndex == 0 => throw new BadHttpRequestException("Первый материал в списке. Выше некуда."),
            DirectionSortEnum.DOWN when materialIndex == materials.Length - 1 => throw new BadHttpRequestException("Последний материал в списке. Ниже некуда."),
            DirectionSortEnum.UP => materialIndex - 1,
            DirectionSortEnum.DOWN => materialIndex + 1,
            _ => throw new BadHttpRequestException("Недопустимое направление сортировки.")
        };

        var adjacentMaterial = materials[adjacentMaterialIndex];
        var materialOrderByCol = material.OrderByCol;
        var adjacentMaterialOrderByCol = adjacentMaterial.OrderByCol;

        var usedPositions = materials.Select(x => x.OrderByCol).ToHashSet();
        var temporaryPosition = 0;
        while (usedPositions.Contains(temporaryPosition))
            temporaryPosition--;
        material.OrderByCol = temporaryPosition;
        await dbContext.SaveChangesAsync(ct);

        adjacentMaterial.OrderByCol = materialOrderByCol;
        await dbContext.SaveChangesAsync(ct);

        material.OrderByCol = adjacentMaterialOrderByCol;
        await dbContext.SaveChangesAsync(ct);

        return new ChangeSortingMaterialCommandResult(true);
    }
}