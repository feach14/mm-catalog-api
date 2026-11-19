using Catalog.Api.Features.Edges.ForAdmin.Stroydvor.Dto;
using Core.CQRS;

namespace Catalog.Api.Features.Edges.ForAdmin.Stroydvor;

public sealed record SyncEdgeStroydvorCommand(int Id) : ICommand<EdgeStroydvorSyncDto>;

public sealed class SyncEdgeStroydvorCommandHandler(
    ICommandHandler<SyncEdgesStroydvorCommand, SyncEdgesStroydvorCommandResult> batchHandler)
    : ICommandHandler<SyncEdgeStroydvorCommand, EdgeStroydvorSyncDto>
{
    public async Task<EdgeStroydvorSyncDto> Handle(SyncEdgeStroydvorCommand command, CancellationToken ct)
    {
        var result = await batchHandler.Handle(new SyncEdgesStroydvorCommand([command.Id]), ct);
        var item = result.Items[0];
        if (!item.Success)
            throw new BadHttpRequestException(item.Error ?? "Не удалось синхронизировать кромку со Стройдвором.");
        return item;
    }
}