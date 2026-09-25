namespace Catalog.Api.Features.MaterialCategories.ForAdmin;

using Core.CQRS;
using Database;
using Database.Entities;
using Dto;

public sealed record CreateMaterialCategoryCommand(MaterialCategoryModel Category) : ICommand<CreateMaterialCategoryCommandResult>;

public sealed record CreateMaterialCategoryCommandResult(
    [property:Description("Id категории материалов")] int Id);

public class CreateMaterialCategoryCommandHandler(CatalogDbContext dbContext) : ICommandHandler<CreateMaterialCategoryCommand, CreateMaterialCategoryCommandResult>
{
    public async Task<CreateMaterialCategoryCommandResult> Handle(CreateMaterialCategoryCommand command, CancellationToken ct) {
        
        var maxOrderByCol = await dbContext.MaterialCategories.MaxAsync(x => (int?)x.OrderByCol, ct) ?? 0;
        
        var materialCategory = new MaterialCategory
        {
            Name = command.Category.Name,
            ExternalLink = command.Category.ExternalLink,
            OrderByCol = maxOrderByCol + 1
        };
        
        dbContext.MaterialCategories.Add(materialCategory);
        await dbContext.SaveChangesAsync(ct);

        return new CreateMaterialCategoryCommandResult(materialCategory.Id);
    }
}
