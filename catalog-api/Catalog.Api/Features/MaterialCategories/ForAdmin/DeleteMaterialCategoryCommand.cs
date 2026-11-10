using Catalog.Api.Services.History;
using Catalog.Database;
using Catalog.Database.Enums;
using Core.CQRS;

namespace Catalog.Api.Features.MaterialCategories.ForAdmin;

public sealed record DeleteMaterialCategoryCommand(int Id) : ICommand<DeleteMaterialCategoryCommandResult>;

public sealed record DeleteMaterialCategoryCommandResult(
    [property: Description("Успех операции")] bool Success);

public class DeleteMaterialCategoryCommandHandler(CatalogDbContext dbContext, ICatalogHistoryWriter historyWriter) : ICommandHandler<DeleteMaterialCategoryCommand, DeleteMaterialCategoryCommandResult>
{
    public async Task<DeleteMaterialCategoryCommandResult> Handle(DeleteMaterialCategoryCommand command, CancellationToken ct)
    {
        var categoryMaterialNames = await dbContext.Materials.AsNoTracking()
            .Where(x => x.CategoryId == command.Id)
            .Select(x => x.Name)
            .Concat(dbContext.ArchivedMaterials.AsNoTracking()
                .Where(x => x.CategoryId == command.Id)
                .Select(x => x.Name))
            .ToArrayAsync(ct);

        if (categoryMaterialNames.Length != 0)
            throw new BadHttpRequestException($"Есть материалы привязанные к указанной категории. Пожалуйста измените категорию у материалов: {string.Join(',', categoryMaterialNames)}.");

        var materialCategory = await dbContext.MaterialCategories.FirstOrDefaultAsync(x => x.Id == command.Id, ct)
            ?? throw new BadHttpRequestException($"Категория материалов с id={command.Id} не существует.");

        dbContext.MaterialCategories.Remove(materialCategory);
        historyWriter.Add(
            CatalogHistoryActionTypeEnum.Delete,
            CatalogHistoryEntityTypeEnum.Category,
            materialCategory.Id,
            $"Удалена категория материалов #{materialCategory.Id} «{materialCategory.Name}». Перед удалением: источник {materialCategory.ExternalLink}.");
        await dbContext.SaveChangesAsync(ct);

        return new DeleteMaterialCategoryCommandResult(true);
    }
}