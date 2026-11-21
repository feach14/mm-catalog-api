using Catalog.Api.Features.Materials.ForAdmin.Dto;
using Catalog.Api.Infrastructure.Ordering;
using Catalog.Api.Services.History;
using Catalog.Database;
using Catalog.Database.Entities;
using Catalog.Database.Enums;
using Core.CQRS;

namespace Catalog.Api.Features.Materials.ForAdmin;

public sealed record AddMaterialCommentModel(
    [property: Description("Текст комментария, не более 50 символов")] string Text,
    [property: Description("Комментарий для клиента")] bool IsForClient = false);

public sealed class AddMaterialCommentModelValidator : AbstractValidator<AddMaterialCommentModel>
{
    public AddMaterialCommentModelValidator()
    {
        RuleFor(model => model.Text)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Введите комментарий.")
            .MaximumLength(50).WithMessage("Комментарий должен содержать не более 50 символов.");
    }
}

public sealed record AddMaterialCommentCommand(int Id, AddMaterialCommentModel Model) : ICommand<MaterialCommentDto>;

public sealed class AddMaterialCommentCommandHandler(CatalogDbContext dbContext, ICatalogHistoryWriter historyWriter)
    : ICommandHandler<AddMaterialCommentCommand, MaterialCommentDto>
{
    public async Task<MaterialCommentDto> Handle(AddMaterialCommentCommand command, CancellationToken ct)
    {
        return await CatalogOrderTransaction.Run(dbContext, async () =>
        {
            var material = await dbContext.Materials.AsNoTracking()
                .Where(material => material.Id == command.Id)
                .Select(material => new { material.Id, material.Name })
                .SingleOrDefaultAsync(ct);
            var archivedMaterial = material is null
                ? await dbContext.ArchivedMaterials.AsNoTracking()
                    .Where(material => material.Id == command.Id)
                    .Select(material => new { material.Id, material.Name })
                    .SingleOrDefaultAsync(ct)
                : null;
            if (material is null && archivedMaterial is null)
                throw new BadHttpRequestException("Материал не найден.");

            var comment = new MaterialComment
            {
                MaterialId = material?.Id,
                ArchivedMaterialId = archivedMaterial?.Id,
                Text = command.Model.Text.Trim(),
                IsForClient = command.Model.IsForClient
            };
            dbContext.MaterialComments.Add(comment);
            historyWriter.Add(CatalogHistoryActionTypeEnum.Update, CatalogHistoryEntityTypeEnum.Material, command.Id,
                $"Материал #{command.Id} «{material?.Name ?? archivedMaterial!.Name}»: добавлен комментарий{(comment.IsForClient ? " для клиента" : string.Empty)} «{comment.Text}».");
            await dbContext.SaveChangesAsync(ct);
            return new MaterialCommentDto(comment.Id, comment.Text, comment.IsForClient);
        }, ct);
    }
}