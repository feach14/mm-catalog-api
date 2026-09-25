using Catalog.Database;
using Core.CQRS;

namespace Catalog.Api.Features.MaterialCategories.ForAdmin;

public sealed record DeleteMaterialCategoryCommand(int Id) : ICommand<DeleteMaterialCategoryCommandResult>;

public sealed record DeleteMaterialCategoryCommandResult(
    [property:Description("Успех операции")] bool Success);

public class DeleteMaterialCategoryCommandHandler(CatalogDbContext dbContext) : ICommandHandler<DeleteMaterialCategoryCommand, DeleteMaterialCategoryCommandResult>
{
    public async Task<DeleteMaterialCategoryCommandResult> Handle(DeleteMaterialCategoryCommand command, CancellationToken ct)
    {
        var categoryMaterials = await dbContext.Materials
            .Where(x => x.CategoryId == command.Id && !x.Deleted)
            .ToArrayAsync(ct);
        
        if (categoryMaterials.Length != 0)
            throw new BadHttpRequestException($"Есть материалы привязанные к указанной категории. Пожалуйста измените категорию у материалов: {string.Join(',', categoryMaterials.Select(x => x.Name).ToArray())}.");

        var materialCategory = await dbContext.MaterialCategories.FirstOrDefaultAsync(x => x.Id == command.Id, ct)
            ?? throw new BadHttpRequestException($"Категория материалов с id={command.Id} не существует.");
        
        dbContext.MaterialCategories.Remove(materialCategory);
        await dbContext.SaveChangesAsync(ct);

        return new DeleteMaterialCategoryCommandResult(true);
    }
}