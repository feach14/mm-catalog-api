using Catalog.Api.Infrastructure.Ordering;
using Catalog.Api.Services.History;
using Catalog.Database;
using Catalog.Database.Entities;
using Catalog.Database.Enums;
using Core.CQRS;

namespace Catalog.Api.Features.Relations.ForAdmin;

public sealed record SetMaterialEdgesCommand(int MaterialId, SetMaterialEdgesModel Model) : ICommand<SetMaterialEdgesCommandResult>;

[Description("Кромки материала в порядке приоритета")]
public sealed record SetMaterialEdgesModel(
    [property: Description("Id кромок в порядке показа; пустой список снимает все связи")] int[] EdgeIds);

public sealed class SetMaterialEdgesModelValidator : AbstractValidator<SetMaterialEdgesModel>
{
    public SetMaterialEdgesModelValidator(CatalogDbContext dbContext, IHttpContextAccessor httpContextAccessor)
    {
        RuleFor(x => x)
            .MustAsync(async (_, ct) =>
            {
                int.TryParse(httpContextAccessor.HttpContext?.Request.RouteValues["materialId"]?.ToString(), out int materialId);
                return await dbContext.Materials.AsNoTracking().Where(x => x.Id == materialId).Select(x => x.Id)
                    .Concat(dbContext.ArchivedMaterials.AsNoTracking().Where(x => x.Id == materialId).Select(x => x.Id))
                    .AnyAsync(ct);
            }).WithMessage("Материал не найден").OverridePropertyName("MaterialId");
        RuleFor(x => x.EdgeIds)
            .Cascade(CascadeMode.Stop)
            .NotNull().WithMessage("Передайте список кромок")
            .Must(ids => ids.All(id => id > 0)).WithMessage("Id кромок должны быть положительными")
            .Must(ids => ids.Distinct().Count() == ids.Length).WithMessage("Кромка не должна повторяться в списке")
            .MustAsync(async (ids, ct) => await dbContext.Edges.AsNoTracking().CountAsync(x => ids.Contains(x.Id), ct) == ids.Length)
            .WithMessage("Одна из кромок не найдена. Обновите список кромок");
    }
}

public sealed record SetMaterialEdgesCommandResult([property: Description("Успех операции")] bool Success);

public sealed class SetMaterialEdgesCommandHandler(CatalogDbContext dbContext, ICatalogHistoryWriter historyWriter)
    : ICommandHandler<SetMaterialEdgesCommand, SetMaterialEdgesCommandResult>
{
    public async Task<SetMaterialEdgesCommandResult> Handle(SetMaterialEdgesCommand command, CancellationToken ct) =>
        await CatalogOrderTransaction.Run(dbContext, async () =>
        {
            var archived = await dbContext.ArchivedMaterials.AsNoTracking().AnyAsync(x => x.Id == command.MaterialId, ct);
            if (!archived && !await dbContext.Materials.AsNoTracking().AnyAsync(x => x.Id == command.MaterialId, ct))
                throw new BadHttpRequestException("Материал не найден. Обновите список материалов.");

            var links = await dbContext.MaterialEdgeLinks
                .Where(x => x.OwnerMaterialId == command.MaterialId)
                .OrderBy(x => x.Priority).ThenBy(x => x.EdgeId)
                .ToArrayAsync(ct);
            var edgeIds = command.Model.EdgeIds;
            var previousIds = links.Select(x => x.EdgeId).ToArray();
            if (previousIds.SequenceEqual(edgeIds))
                return new SetMaterialEdgesCommandResult(true);

            var selectedIds = edgeIds.ToHashSet();
            dbContext.MaterialEdgeLinks.RemoveRange(links.Where(x => !selectedIds.Contains(x.EdgeId)));
            var existing = links.ToDictionary(x => x.EdgeId);
            for (var index = 0; index < edgeIds.Length; index++)
            {
                var edgeId = edgeIds[index];
                if (existing.TryGetValue(edgeId, out var link))
                    link.Priority = index + 1;
                else
                    dbContext.MaterialEdgeLinks.Add(new MaterialEdgeLink
                    {
                        MaterialId = archived ? null : command.MaterialId,
                        ArchivedMaterialId = archived ? command.MaterialId : null,
                        EdgeId = edgeId,
                        Priority = index + 1
                    });
            }

            historyWriter.Add(
                CatalogHistoryActionTypeEnum.Update, CatalogHistoryEntityTypeEnum.MaterialEdgeRelation, command.MaterialId,
                $"Изменены кромки материала #{command.MaterialId}: [{string.Join(", ", previousIds)}] → [{string.Join(", ", edgeIds)}]. Порядок соответствует приоритету показа.");
            await dbContext.SaveChangesAsync(ct);
            return new SetMaterialEdgesCommandResult(true);
        }, ct);
}