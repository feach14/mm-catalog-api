using Catalog.Api.Features.History;
using Catalog.Api.Features.MaterialCategories.ForAdmin.Dto;
using Catalog.Database;
using Catalog.Database.Entities;
using Catalog.Database.Enums;
using Core.CQRS;

namespace Catalog.Api.Features.MaterialCategories.ForAdmin;

public sealed record CreateMaterialCategoryCommand(MaterialCategoryModel Category) : ICommand<CreateMaterialCategoryCommandResult>;

public sealed record CreateMaterialCategoryCommandResult(
    [property: Description("Id категории материалов")] int Id);

public class CreateMaterialCategoryCommandHandler(CatalogDbContext dbContext, ICatalogHistoryWriter historyWriter) : ICommandHandler<CreateMaterialCategoryCommand, CreateMaterialCategoryCommandResult>
{
    public async Task<CreateMaterialCategoryCommandResult> Handle(CreateMaterialCategoryCommand command, CancellationToken ct)
    {
        var maxOrderByCol = await dbContext.MaterialCategories.MaxAsync(x => (int?)x.OrderByCol, ct) ?? 0;

        var materialCategory = new MaterialCategory
        {
            Name = command.Category.Name,
            ExternalLink = command.Category.ExternalLink,
            OrderByCol = maxOrderByCol + 1
        };

        await using var transaction = await dbContext.Database.BeginTransactionAsync(ct);
        dbContext.MaterialCategories.Add(materialCategory);
        await dbContext.SaveChangesAsync(ct);
        historyWriter.Add(
            CatalogHistoryActionType.Create,
            CatalogHistoryEntityType.Category,
            materialCategory.Id,
            $"Создана категория материалов #{materialCategory.Id} «{materialCategory.Name}»: источник {materialCategory.ExternalLink}.");
        await dbContext.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        return new CreateMaterialCategoryCommandResult(materialCategory.Id);
    }
}