using Catalog.Api.Features.Materials.ForAdmin.Stroydvor.Dto;
using Core.CQRS;

namespace Catalog.Api.Features.Materials.ForAdmin.Stroydvor;

public sealed record SyncMaterialStroydvorCommand(int Id) : ICommand<MaterialStroydvorSyncDto>;

public sealed class SyncMaterialStroydvorCommandHandler(
    ICommandHandler<SyncMaterialsStroydvorCommand, SyncMaterialsStroydvorCommandResult> batchHandler)
    : ICommandHandler<SyncMaterialStroydvorCommand, MaterialStroydvorSyncDto>
{
    public async Task<MaterialStroydvorSyncDto> Handle(SyncMaterialStroydvorCommand command, CancellationToken ct)
    {
        var result = await batchHandler.Handle(new SyncMaterialsStroydvorCommand([command.Id]), ct);
        return result.Items[0];
    }
}