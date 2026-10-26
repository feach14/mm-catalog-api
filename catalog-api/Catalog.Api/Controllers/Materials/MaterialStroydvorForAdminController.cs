using Catalog.Api.Features.Materials.ForAdmin.Stroydvor;
using Catalog.Api.Features.Materials.ForAdmin.Stroydvor.Dto;
using Core.Attributes;
using Core.Controllers;
using Core.CQRS;

namespace Catalog.Api.Controllers.Materials;

[Route("api/for-admin/materials"), OpenApiTagOrder(2)]
public sealed class MaterialStroydvorForAdminController : BaseAdministrationApiController
{
    [HttpPost("{id:int}/stroydvor/sync"),
     EndpointSummary(nameof(SyncMaterial)),
     EndpointDescription("Синхронизация материала со Стройдвором"),
     ProducesResponseType(typeof(MaterialStroydvorSyncDto), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Результат синхронизации")]
    public Task<MaterialStroydvorSyncDto> SyncMaterial(
        [FromRoute, Description("Id материала")] int id,
        [FromServices] ICommandHandler<SyncMaterialStroydvorCommand, MaterialStroydvorSyncDto> handler) =>
        handler.Handle(new SyncMaterialStroydvorCommand(id), HttpContext.RequestAborted);

    [HttpPost("stroydvor/sync"),
     EndpointSummary(nameof(SyncMaterials)),
     EndpointDescription("Синхронизация списка материалов со Стройдвором"),
     ProducesResponseType(typeof(SyncMaterialsStroydvorCommandResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Результаты синхронизации")]
    public Task<SyncMaterialsStroydvorCommandResult> SyncMaterials(
        [FromBody, Description("Материалы для синхронизации")] SyncMaterialsStroydvorCommand command,
        [FromServices] ICommandHandler<SyncMaterialsStroydvorCommand, SyncMaterialsStroydvorCommandResult> handler) =>
        handler.Handle(command, HttpContext.RequestAborted);
}