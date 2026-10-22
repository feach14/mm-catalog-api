using Catalog.Api.Services.History;
using Catalog.Database;
using Catalog.Database.Entities;
using Catalog.Database.Enums;
using Core.CQRS;

namespace Catalog.Api.Features.Relations.ForAdmin;

public sealed record UpdateEdgeMaterialsModel(
    [property: Description("Изменения связей с материалами")] EdgeMaterialChangeModel[] Changes);

public sealed record EdgeMaterialChangeModel(
    [property: Description("Id материала")] int MaterialId,
    [property: Description("Наличие связи"), JsonRequired] bool Linked,
    [property: Description("Id маркировок; null сохраняет текущие назначения")] int[]? MarkingIds = null);

public sealed record UpdateEdgeMaterialsCommand(int EdgeId, UpdateEdgeMaterialsModel Model)
    : ICommand<UpdateEdgeMaterialsCommandResult>;

public sealed record UpdateEdgeMaterialsCommandResult(
    [property: Description("Количество изменённых материалов")] int ChangedCount);

public sealed class UpdateEdgeMaterialsModelValidator : AbstractValidator<UpdateEdgeMaterialsModel>
{
    public UpdateEdgeMaterialsModelValidator(CatalogDbContext dbContext, IHttpContextAccessor httpContextAccessor)
    {
        RuleFor(x => x)
            .MustAsync(async (_, ct) =>
            {
                var edgeId = int.TryParse(httpContextAccessor.HttpContext?.Request.RouteValues["edgeId"]?.ToString(), out var id) ? id : 0;
                return await dbContext.Edges.AsNoTracking().AnyAsync(x => x.Id == edgeId, ct);
            }).WithMessage("Кромка не найдена").OverridePropertyName("EdgeId");
        RuleFor(x => x.Changes)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithMessage("Передайте список изменений связей")
            .Must(changes => changes.All(change => change is not null)).WithMessage("Список изменений не должен содержать пустые элементы")
            .Must(changes => changes.All(change => change.MaterialId > 0)).WithMessage("Id материалов должны быть больше нуля")
            .Must(changes => changes.Select(change => change.MaterialId).Distinct().Count() == changes.Length).WithMessage("Материал не должен повторяться в списке изменений")
            .MustAsync(async (changes, ct) =>
            {
                var materialIds = changes.Select(change => change.MaterialId).ToArray();
                return materialIds.Length == 0 || await dbContext.Materials.AsNoTracking()
                    .CountAsync(x => materialIds.Contains(x.Id), ct) == materialIds.Length;
            }).WithMessage("Один или несколько материалов не найдены")
            .Must(changes => changes.All(change => change.MarkingIds is null || change.MarkingIds.All(id => id > 0))).WithMessage("Id маркировок должны быть больше нуля")
            .Must(changes => changes.All(change => change.MarkingIds is null || change.MarkingIds.Distinct().Count() == change.MarkingIds.Length)).WithMessage("Маркировка не должна повторяться в одной связи")
            .Must(changes => changes.All(change => change.Linked || change.MarkingIds is null or { Length: 0 })).WithMessage("При удалении связи нельзя назначать маркировки")
            .MustAsync(async (changes, ct) =>
            {
                var markingIds = changes.SelectMany(change => change.MarkingIds ?? []).Distinct().ToArray();
                return markingIds.Length == 0 || await dbContext.RelationMarkings.AsNoTracking()
                    .CountAsync(x => markingIds.Contains(x.Id), ct) == markingIds.Length;
            }).WithMessage("Одна или несколько маркировок не найдены");
    }
}

public sealed class UpdateEdgeMaterialsCommandHandler(CatalogDbContext dbContext, ICatalogHistoryWriter historyWriter)
    : ICommandHandler<UpdateEdgeMaterialsCommand, UpdateEdgeMaterialsCommandResult>
{
    public async Task<UpdateEdgeMaterialsCommandResult> Handle(UpdateEdgeMaterialsCommand command, CancellationToken ct)
    {
        var changes = command.Model.Changes;
        if (changes.Length == 0) return new UpdateEdgeMaterialsCommandResult(0);

        var materialIds = changes.Select(change => change.MaterialId).Order().ToArray();
        await using var transaction = await dbContext.Database.BeginTransactionAsync(ct);
        var lockedMaterialIds = await dbContext.Database.SqlQuery<int>(
                $"SELECT id AS \"Value\" FROM catalog.materials WHERE id = ANY({materialIds}) ORDER BY id FOR UPDATE")
            .ToArrayAsync(ct);
        if (lockedMaterialIds.Length != materialIds.Length)
            throw new BadHttpRequestException("Один или несколько материалов были удалены. Обновите список материалов.");

        var edgeName = await dbContext.Edges.AsNoTracking()
            .Where(x => x.Id == command.EdgeId).Select(x => x.Name).SingleAsync(ct);
        var links = await dbContext.MaterialEdgeLinks
            .Include(x => x.Markings)
            .Where(x => materialIds.Contains(x.MaterialId))
            .OrderBy(x => x.MaterialId).ThenBy(x => x.Priority).ThenBy(x => x.EdgeId)
            .ToArrayAsync(ct);
        var materialLinksById = links.GroupBy(x => x.MaterialId).ToDictionary(group => group.Key, group => group.ToList());
        var changedCount = 0;

        foreach (var change in changes.OrderBy(x => x.MaterialId))
        {
            var materialLinks = materialLinksById.GetValueOrDefault(change.MaterialId) ?? [];
            var link = materialLinks.SingleOrDefault(x => x.EdgeId == command.EdgeId);
            string description;
            if (!change.Linked)
            {
                if (link is null) continue;
                dbContext.MaterialEdgeLinks.Remove(link);
                materialLinks.Remove(link);
                description = "связь удалена";
            }
            else if (link is null)
            {
                link = new MaterialEdgeLink
                {
                    MaterialId = change.MaterialId,
                    EdgeId = command.EdgeId,
                    Priority = materialLinks.Count + 1
                };
                foreach (var markingId in change.MarkingIds ?? [])
                    link.Markings.Add(new MaterialEdgeLinkMarking
                    {
                        MaterialId = change.MaterialId,
                        EdgeId = command.EdgeId,
                        MarkingId = markingId
                    });
                dbContext.MaterialEdgeLinks.Add(link);
                materialLinks.Add(link);
                description = $"связь добавлена, маркировки: {MarkingIdsText(change.MarkingIds ?? [])}";
            }
            else
            {
                if (change.MarkingIds is null) continue;
                var previousMarkingIds = link.Markings.Select(x => x.MarkingId).ToHashSet();
                var requestedMarkingIds = change.MarkingIds.ToHashSet();
                if (previousMarkingIds.SetEquals(requestedMarkingIds)) continue;

                foreach (var assignment in link.Markings.Where(x => !requestedMarkingIds.Contains(x.MarkingId)).ToArray())
                {
                    link.Markings.Remove(assignment);
                    dbContext.MaterialEdgeLinkMarkings.Remove(assignment);
                }
                foreach (var markingId in requestedMarkingIds.Except(previousMarkingIds))
                {
                    var assignment = new MaterialEdgeLinkMarking
                    {
                        MaterialId = change.MaterialId,
                        EdgeId = command.EdgeId,
                        MarkingId = markingId
                    };
                    link.Markings.Add(assignment);
                    dbContext.MaterialEdgeLinkMarkings.Add(assignment);
                }
                description = $"маркировки изменены: {MarkingIdsText(previousMarkingIds)} → {MarkingIdsText(requestedMarkingIds)}";
            }

            for (var index = 0; index < materialLinks.Count; index++)
                materialLinks[index].Priority = index + 1;
            historyWriter.Add(
                CatalogHistoryActionTypeEnum.Update, CatalogHistoryEntityTypeEnum.MaterialEdgeRelation, change.MaterialId,
                $"Материал #{change.MaterialId}, кромка #{command.EdgeId} «{edgeName}»: {description}.");
            changedCount++;
        }

        if (changedCount > 0) await dbContext.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
        return new UpdateEdgeMaterialsCommandResult(changedCount);
    }

    private static string MarkingIdsText(IEnumerable<int> markingIds)
    {
        var ids = markingIds.Order().ToArray();
        return ids.Length == 0 ? "нет" : string.Join(", ", ids);
    }
}