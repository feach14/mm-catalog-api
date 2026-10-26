using Catalog.Api.Services.History;
using Catalog.Database;
using Catalog.Database.Enums;
using Core.CQRS;

namespace Catalog.Api.Features.Materials.ForAdmin;

public sealed record UpdateMaterialExternalLinkModel(
    [property: Description("Ссылка на внешний источник")] string? ExternalLink);

public sealed class UpdateMaterialExternalLinkModelValidator : AbstractValidator<UpdateMaterialExternalLinkModel>
{
    public UpdateMaterialExternalLinkModelValidator()
    {
        RuleFor(model => model.ExternalLink)
            .Cascade(CascadeMode.Stop)
            .MaximumLength(250).WithMessage("Ссылка должна содержать не более 250 символов.")
            .Must(link => string.IsNullOrWhiteSpace(link)
                || (Uri.TryCreate(link, UriKind.Absolute, out var uri)
                    && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)))
            .WithMessage("Укажите полную ссылку с http:// или https://.");
    }
}

public sealed record UpdateMaterialExternalLinkCommand(int Id, UpdateMaterialExternalLinkModel Model)
    : ICommand<UpdateMaterialExternalLinkCommandResult>;

public sealed record UpdateMaterialExternalLinkCommandResult(
    [property: Description("Успех операции")] bool Success);

public sealed class UpdateMaterialExternalLinkCommandHandler(CatalogDbContext dbContext, ICatalogHistoryWriter historyWriter)
    : ICommandHandler<UpdateMaterialExternalLinkCommand, UpdateMaterialExternalLinkCommandResult>
{
    public async Task<UpdateMaterialExternalLinkCommandResult> Handle(UpdateMaterialExternalLinkCommand command, CancellationToken ct)
    {
        var material = await dbContext.Materials.SingleOrDefaultAsync(material => material.Id == command.Id, ct)
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