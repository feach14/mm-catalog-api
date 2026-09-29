using Catalog.Api.Features.MaterialThicknesses.ForAdmin.Dto;
using Catalog.Api.Services.History;
using Catalog.Database;
using Catalog.Database.Enums;
using Core.CQRS;

namespace Catalog.Api.Features.MaterialThicknesses.ForAdmin;

public sealed record UpdateMaterialThicknessCommand(int Id, UpdateMaterialThicknessModel Model) : ICommand<UpdateMaterialThicknessCommandResult>;
public sealed record UpdateMaterialThicknessModel : MaterialThicknessModel;

public sealed class UpdateMaterialThicknessModelValidator : AbstractValidator<UpdateMaterialThicknessModel>
{
    public UpdateMaterialThicknessModelValidator(CatalogDbContext dbContext, IHttpContextAccessor httpContextAccessor)
    {
        ClassLevelCascadeMode = CascadeMode.Stop;
        RuleFor(x => x)
            .MustAsync(async (_, ct) =>
            {
                var routeId = httpContextAccessor.HttpContext?.Request.RouteValues["id"]?.ToString();
                return int.TryParse(routeId, out var id) && id > 0
                    && await dbContext.MaterialThicknesses.AsNoTracking().AnyAsync(x => x.Id == id, ct);
            }).WithMessage("Указанная толщина материала не найдена")
            .OverridePropertyName("id");
        Include(new MaterialThicknessModelValidator(dbContext, httpContextAccessor));
    }
}

public sealed record UpdateMaterialThicknessCommandResult(
    [property: Description("Успех операции")] bool Success);

public sealed class UpdateMaterialThicknessCommandHandler(CatalogDbContext dbContext, ICatalogHistoryWriter historyWriter)
    : ICommandHandler<UpdateMaterialThicknessCommand, UpdateMaterialThicknessCommandResult>
{
    public async Task<UpdateMaterialThicknessCommandResult> Handle(UpdateMaterialThicknessCommand command, CancellationToken ct)
    {
        var thickness = await dbContext.MaterialThicknesses.SingleAsync(x => x.Id == command.Id, ct);
        var name = command.Model.Name.Trim();
        var changes = new List<string>();
        if (thickness.Name != name)
            changes.Add($"название «{thickness.Name}» → «{name}»");
        if (thickness.Value != command.Model.Value)
            changes.Add($"толщина {thickness.Value} → {command.Model.Value} мм");

        if (changes.Count == 0)
            return new UpdateMaterialThicknessCommandResult(true);

        thickness.Name = name;
        thickness.Value = command.Model.Value;
        historyWriter.Add(
            CatalogHistoryActionTypeEnum.Update,
            CatalogHistoryEntityTypeEnum.Thickness,
            thickness.Id,
            $"Толщина материала #{thickness.Id} изменена: {string.Join(", ", changes)}.");
        await dbContext.SaveChangesAsync(ct);
        return new UpdateMaterialThicknessCommandResult(true);
    }
}