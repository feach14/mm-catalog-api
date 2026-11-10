using Catalog.Api.Services.History;
using Catalog.Database;
using Catalog.Database.Enums;
using Core.CQRS;

namespace Catalog.Api.Features.Materials.ForAdmin;

public sealed record UpdateArchivedMaterialExternalLinkCommand(int Id, UpdateMaterialExternalLinkModel Model) : ICommand<UpdateMaterialExternalLinkCommandResult>;

public sealed class UpdateArchivedMaterialExternalLinkCommandHandler(CatalogDbContext dbContext, ICatalogHistoryWriter historyWriter)
    : ICommandHandler<UpdateArchivedMaterialExternalLinkCommand, UpdateMaterialExternalLinkCommandResult>
{
    public async Task<UpdateMaterialExternalLinkCommandResult> Handle(UpdateArchivedMaterialExternalLinkCommand command, CancellationToken ct)
    {
        var material = await dbContext.ArchivedMaterials.SingleOrDefaultAsync(material => material.Id == command.Id, ct)
            ?? throw new BadHttpRequestException("Материал не найден.");
        var externalLink = string.IsNullOrWhiteSpace(command.Model.ExternalLink) ? null : command.Model.ExternalLink.Trim();
        if (material.ExternalLink == externalLink)
            return new UpdateMaterialExternalLinkCommandResult(true);

        var previousLink = material.ExternalLink;
        material.ExternalLink = externalLink;
        historyWriter.Add(CatalogHistoryActionTypeEnum.Update, CatalogHistoryEntityTypeEnum.Material, material.Id,
            $"Материал #{material.Id} «{material.Name}» изменён: ссылка «{previousLink ?? "нет"}» → «{externalLink ?? "нет"}».");
        await dbContext.SaveChangesAsync(ct);
        return new UpdateMaterialExternalLinkCommandResult(true);
    }
}