using Catalog.Api.Infrastructure.Ordering;
using Catalog.Database;
using Core.BaseEnums;
using Core.CQRS;

namespace Catalog.Api.Features.Materials.ForAdmin;

public record ChangeSortingMaterialCommand(
    [property: Description("Id материала")] int Id,
    [property: Description("Направление: UP — выше, DOWN — ниже"), JsonConverter(typeof(StrictStringEnumConverter))] DirectionSortEnum Direction)
    : ICommand<ChangeSortingMaterialCommandResult>;

public sealed class ChangeSortingMaterialCommandValidator : AbstractValidator<ChangeSortingMaterialCommand>
{
    public ChangeSortingMaterialCommandValidator(CatalogDbContext dbContext)
    {
        RuleFor(x => x.Id).Cascade(CascadeMode.Stop)
            .GreaterThan(0).WithMessage("Id материала должен быть больше нуля")
            .MustAsync(async (id, ct) => await dbContext.Materials.AsNoTracking().AnyAsync(x => x.Id == id, ct)).WithMessage("Указанный материал не найден.");
        RuleFor(x => x.Direction).IsInEnum().WithMessage("Указано недопустимое направление сортировки");
    }
}

public record ChangeSortingMaterialCommandResult(
    [property: Description("Успех операции")] bool Success);

public class ChangeSortingMaterialCommandHandler(CatalogDbContext dbContext) : ICommandHandler<ChangeSortingMaterialCommand, ChangeSortingMaterialCommandResult>
{
    public async Task<ChangeSortingMaterialCommandResult> Handle(ChangeSortingMaterialCommand command, CancellationToken ct)
    {
        return await CatalogOrderTransaction.Run(dbContext, async () =>
        {
            var materials = await dbContext.Materials.OrderBy(x => x.OrderByCol).ThenBy(x => x.Id).ToArrayAsync(ct);
            var material = materials.Single(x => x.Id == command.Id);
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
        }, ct);
    }
}