using Catalog.Api.Features.MaterialCategories.ForAdmin.Dto;
using Catalog.Api.Services.History;
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
            HideOnSite = command.Category.HideOnSite,
            OrderByCol = maxOrderByCol + 1
        };

        await using var transaction = await dbContext.Database.BeginTransactionAsync(ct);
        dbContext.MaterialCategories.Add(materialCategory);
        await dbContext.SaveChangesAsync(ct);
        historyWriter.Add(
            CatalogHistoryActionTypeEnum.Create,
            CatalogHistoryEntityTypeEnum.Category,
            materialCategory.Id,
            $"Создана категория материалов #{materialCategory.Id} «{materialCategory.Name}»: источник {materialCategory.ExternalLink}, скрыта на сайте: {materialCategory.HideOnSite}.");
        await dbContext.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        return new CreateMaterialCategoryCommandResult(materialCategory.Id);
    }
}