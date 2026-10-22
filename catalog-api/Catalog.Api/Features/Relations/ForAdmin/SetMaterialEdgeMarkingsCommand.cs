using Catalog.Api.Services.History;
using Catalog.Database;
using Catalog.Database.Entities;
using Catalog.Database.Enums;
using Core.CQRS;

namespace Catalog.Api.Features.Relations.ForAdmin;

public sealed record SetMaterialEdgeMarkingsCommand(int MaterialId, int EdgeId, SetMaterialEdgeMarkingsModel Model)
    : ICommand<SetMaterialEdgeMarkingsCommandResult>;

[Description("Маркировки связи материала с кромкой")]
public sealed record SetMaterialEdgeMarkingsModel(
    [property: Description("Id маркировок; пустой список снимает все маркировки")] int[] MarkingIds);

public sealed class SetMaterialEdgeMarkingsModelValidator : AbstractValidator<SetMaterialEdgeMarkingsModel>
{
    public SetMaterialEdgeMarkingsModelValidator(CatalogDbContext dbContext, IHttpContextAccessor httpContextAccessor)
    {
        RuleFor(x => x)
            .MustAsync(async (_, ct) =>
            {
                var routeValues = httpContextAccessor.HttpContext?.Request.RouteValues;
                int.TryParse(routeValues?["materialId"]?.ToString(), out int materialId);
                int.TryParse(routeValues?["edgeId"]?.ToString(), out int edgeId);
                return await dbContext.MaterialEdgeLinks.AsNoTracking()
                    .AnyAsync(x => x.MaterialId == materialId && x.EdgeId == edgeId, ct);
            }).WithMessage("Связь материала с кромкой не найдена").OverridePropertyName("EdgeId");
        RuleFor(x => x.MarkingIds)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithMessage("Передайте список маркировок")
            .Must(ids => ids.All(id => id > 0)).WithMessage("Id маркировок должны быть положительными")
            .Must(ids => ids.Distinct().Count() == ids.Length).WithMessage("Маркировка не должна повторяться в списке")
            .MustAsync(async (ids, ct) => await dbContext.RelationMarkings.AsNoTracking().CountAsync(x => ids.Contains(x.Id), ct) == ids.Length)
            .WithMessage("Одна из маркировок не найдена. Обновите список маркировок");
    }
}

public sealed record SetMaterialEdgeMarkingsCommandResult([property: Description("Успех операции")] bool Success);

public sealed class SetMaterialEdgeMarkingsCommandHandler(CatalogDbContext dbContext, ICatalogHistoryWriter historyWriter)
    : ICommandHandler<SetMaterialEdgeMarkingsCommand, SetMaterialEdgeMarkingsCommandResult>
{
    public async Task<SetMaterialEdgeMarkingsCommandResult> Handle(SetMaterialEdgeMarkingsCommand command, CancellationToken ct)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(ct);
        var materialId = await dbContext.Database.SqlQuery<int>(
            $"SELECT id AS \"Value\" FROM catalog.materials WHERE id = {command.MaterialId} FOR UPDATE").SingleOrDefaultAsync(ct);
        if (materialId == 0) throw new BadHttpRequestException("Материал не найден. Обновите список материалов.");
        var link = await dbContext.MaterialEdgeLinks.Include(x => x.Markings)
            .SingleOrDefaultAsync(x => x.MaterialId == command.MaterialId && x.EdgeId == command.EdgeId, ct)
            ?? throw new BadHttpRequestException("Связь материала с кромкой не найдена. Обновите список кромок.");
        var previousIds = link.Markings.Select(x => x.MarkingId).Order().ToArray();
        var markingIds = command.Model.MarkingIds.Order().ToArray();
        if (previousIds.SequenceEqual(markingIds))
        {
            await transaction.CommitAsync(ct);
            return new SetMaterialEdgeMarkingsCommandResult(true);
        }

        var selectedIds = markingIds.ToHashSet();
        dbContext.MaterialEdgeLinkMarkings.RemoveRange(link.Markings.Where(x => !selectedIds.Contains(x.MarkingId)));
        var existingIds = previousIds.ToHashSet();
        foreach (int markingId in markingIds.Where(id => !existingIds.Contains(id)))
            link.Markings.Add(new MaterialEdgeLinkMarking
            {
                MaterialId = command.MaterialId,
                EdgeId = command.EdgeId,
                MarkingId = markingId
            });

        historyWriter.Add(
            CatalogHistoryActionTypeEnum.Update, CatalogHistoryEntityTypeEnum.MaterialEdgeRelation, command.MaterialId,
            $"Изменены маркировки кромки #{command.EdgeId} у материала #{command.MaterialId}: [{string.Join(", ", previousIds)}] → [{string.Join(", ", markingIds)}].");
        await dbContext.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return new SetMaterialEdgeMarkingsCommandResult(true);
    }
}