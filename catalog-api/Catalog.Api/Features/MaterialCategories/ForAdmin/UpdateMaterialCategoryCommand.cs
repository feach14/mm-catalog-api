using Catalog.Api.Features.MaterialCategories.ForAdmin.Dto;
using Catalog.Database;
using Core.CQRS;

namespace Catalog.Api.Features.MaterialCategories.ForAdmin;

public sealed record UpdateMaterialCategoryCommand(int Id, MaterialCategoryModel Category) : ICommand<UpdateMaterialCategoryCommandResult>;

public sealed record UpdateMaterialCategoryCommandResult(
    [property:Description("Успех операции")] bool Success);

public class UpdateMaterialCategoryCommandHandler(CatalogDbContext dbContext) :ICommandHandler<UpdateMaterialCategoryCommand, UpdateMaterialCategoryCommandResult>
{
    public async Task<UpdateMaterialCategoryCommandResult> Handle(UpdateMaterialCategoryCommand command, CancellationToken ct)
    {
        var materialCategory = await dbContext.MaterialCategories.FirstOrDefaultAsync(x => x.Id == command.Id, ct)
            ?? throw new BadHttpRequestException($"Категория материалов с id={command.Id} не существует.");
        
        materialCategory.Name = command.Category.Name;
        materialCategory.ExternalLink = command.Category.ExternalLink;
        
        dbContext.MaterialCategories.Update(materialCategory);
        await dbContext.SaveChangesAsync(ct);

        return new UpdateMaterialCategoryCommandResult(true);
    }
}