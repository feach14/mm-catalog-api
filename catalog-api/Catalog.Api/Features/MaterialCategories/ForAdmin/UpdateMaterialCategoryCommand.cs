using Catalog.Api.Features.History;
using Catalog.Api.Features.MaterialCategories.ForAdmin.Dto;
using Catalog.Database;
using Catalog.Database.Enums;
using Core.CQRS;

namespace Catalog.Api.Features.MaterialCategories.ForAdmin;

public sealed record UpdateMaterialCategoryCommand(int Id, MaterialCategoryModel Category) : ICommand<UpdateMaterialCategoryCommandResult>;

public sealed record UpdateMaterialCategoryCommandResult(
    [property: Description("Успех операции")] bool Success);

public class UpdateMaterialCategoryCommandHandler(CatalogDbContext dbContext, ICatalogHistoryWriter historyWriter) : ICommandHandler<UpdateMaterialCategoryCommand, UpdateMaterialCategoryCommandResult>
{
    public async Task<UpdateMaterialCategoryCommandResult> Handle(UpdateMaterialCategoryCommand command, CancellationToken ct)
    {
        var materialCategory = await dbContext.MaterialCategories.FirstOrDefaultAsync(x => x.Id == command.Id, ct)
            ?? throw new BadHttpRequestException($"Категория материалов с id={command.Id} не существует.");

        var changes = new List<string>();
        if (materialCategory.Name != command.Category.Name)
            changes.Add($"название «{materialCategory.Name}» → «{command.Category.Name}»");
        if (materialCategory.ExternalLink != command.Category.ExternalLink)
            changes.Add($"источник «{materialCategory.ExternalLink}» → «{command.Category.ExternalLink}»");

        if (changes.Count == 0)
            return new UpdateMaterialCategoryCommandResult(true);

        materialCategory.Name = command.Category.Name;
        materialCategory.ExternalLink = command.Category.ExternalLink;
        dbContext.MaterialCategories.Update(materialCategory);
        historyWriter.Add(
            CatalogHistoryActionType.Update,
            CatalogHistoryEntityType.Category,
            materialCategory.Id,
            $"Категория материалов #{materialCategory.Id} изменена: {string.Join(", ", changes)}.");
        await dbContext.SaveChangesAsync(ct);

        return new UpdateMaterialCategoryCommandResult(true);
    }
}