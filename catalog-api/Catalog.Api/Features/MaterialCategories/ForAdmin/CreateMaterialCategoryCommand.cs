using Catalog.Api.Features.MaterialCategories.ForAdmin.Dto;
using Catalog.Database;
using Catalog.Database.Entities;
using Core.CQRS;

namespace Catalog.Api.Features.MaterialCategories.ForAdmin;

public sealed record CreateMaterialCategoryCommand(MaterialCategoryModel Category) : ICommand<CreateMaterialCategoryCommandResult>;

public sealed record CreateMaterialCategoryCommandResult(
    [property:Description("Id категории материалов")] int Id);

public class CreateMaterialCategoryCommandHandler(CatalogDbContext dbContext) : ICommandHandler<CreateMaterialCategoryCommand, CreateMaterialCategoryCommandResult>
{
    public async Task<CreateMaterialCategoryCommandResult> Handle(CreateMaterialCategoryCommand command, CancellationToken ct)
    {
        var materialCategory = new MaterialCategory
        {
            Name = command.Category.Name,
            ExternalLink = command.Category.ExternalLink,
            OrderByCol = await dbContext.MaterialCategories.MaxAsync(x => x.OrderByCol, ct) + 1
        };
        dbContext.MaterialCategories.Add(materialCategory);
        await dbContext.SaveChangesAsync(ct);

        return new CreateMaterialCategoryCommandResult(materialCategory.Id);
    }
}