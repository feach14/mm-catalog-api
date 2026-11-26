using System.Text.Encodings.Web;
using System.Text.Json;
using Catalog.Api.Features.Edges.ForAdmin;
using Catalog.Api.Features.Edges.ForAdmin.Dto;
using Catalog.Api.Features.Edges.ForAdmin.Stroydvor;
using Catalog.Api.Features.Edges.ForAdmin.Stroydvor.Dto;
using Catalog.Api.Features.Exports.Dto;
using Catalog.Api.Infrastructure.Excel;
using Core.Attributes;
using Core.Controllers;
using Core.CQRS;
using Core.RequestResponseLogger;

namespace Catalog.Api.Controllers.Edges;

[Route("api/for-admin/edges"),
 OpenApiTagOrder(11)]
public sealed class EdgesForAdminController : BaseAdministrationApiController
{
    [HttpPost("change-order-col"),
     EndpointSummary(nameof(ChangeOrderCol)),
     EndpointDescription("Изменение порядка кромок"),
     ProducesResponseType(typeof(ChangeSortingEdgeCommandResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Результат команды")]
    public Task<ChangeSortingEdgeCommandResult> ChangeOrderCol(
        [FromBody, Description("Параметры сортировки")] ChangeSortingEdgeCommand command,
        [FromServices] ICommandHandler<ChangeSortingEdgeCommand, ChangeSortingEdgeCommandResult> handler) =>
        handler.Handle(command, HttpContext.RequestAborted);

    [HttpGet("export/columns"),
     EndpointSummary(nameof(ExportColumns)),
     EndpointDescription("Поля выгрузки кромок в Excel"),
     ProducesResponseType(typeof(ExcelExportColumnsResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Поля выгрузки")]
    public Task<ExcelExportColumnsResult> ExportColumns(
        [FromServices] IQueryHandler<GetEdgeExportColumnsQuery, ExcelExportColumnsResult> handler) =>
        handler.Handle(new GetEdgeExportColumnsQuery(), HttpContext.RequestAborted);

    [HttpPost("export"),
     SkipResponseBodyLogging,
     EndpointSummary(nameof(Export)),
     EndpointDescription("Выгрузка кромок в Excel"),
     ProducesResponseType(typeof(byte[]), StatusCodes.Status200OK, ExcelExportFile.ContentType, Description = "Файл Excel")]
    public async Task<IActionResult> Export(
        [FromBody, Description("Параметры выгрузки")] ExportEdgesForAdminQuery query,
        [FromServices] IQueryHandler<ExportEdgesForAdminQuery, ExcelExportFile> handler)
    {
        var result = await handler.Handle(query, HttpContext.RequestAborted);
        Response.Headers.CacheControl = "no-store";
        return File(result.Content, ExcelExportFile.ContentType, result.FileName);
    }

    [HttpGet("compact"),
     EndpointSummary(nameof(CompactEdges)),
     EndpointDescription("Список кромок для административной панели"),
     ProducesResponseType(typeof(GetCompactEdgesForAdminQueryResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Кромки и справочники")]
    public async Task<JsonResult> CompactEdges(
        [FromServices] IQueryHandler<GetCompactEdgesForAdminQuery, GetCompactEdgesForAdminQueryResult> handler,
        [FromServices] IOptions<JsonOptions> jsonOptions)
    {
        var result = await handler.Handle(new GetCompactEdgesForAdminQuery(), HttpContext.RequestAborted);
        return new JsonResult(result, new JsonSerializerOptions(jsonOptions.Value.JsonSerializerOptions)
        {
            WriteIndented = false,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });
    }

    [HttpGet("{id:int}"),
     EndpointSummary(nameof(Edge)),
     EndpointDescription("Информация о кромке"),
     ProducesResponseType(typeof(GetEdgeForAdminQueryResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Кромка")]
    public Task<GetEdgeForAdminQueryResult> Edge(
        [FromRoute, Description("Id кромки")] int id,
        [FromServices] IQueryHandler<GetEdgeForAdminQuery, GetEdgeForAdminQueryResult> handler) =>
        handler.Handle(new GetEdgeForAdminQuery(id), HttpContext.RequestAborted);

    [HttpPost,
     EndpointSummary(nameof(CreateEdge)),
     EndpointDescription("Создание кромки"),
     ProducesResponseType(typeof(CreateEdgeCommandResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Id созданной кромки")]
    public Task<CreateEdgeCommandResult> CreateEdge(
        [FromBody, Description("Параметры кромки")] CreateEdgeModel model,
        [FromServices] ICommandHandler<CreateEdgeCommand, CreateEdgeCommandResult> handler) =>
        handler.Handle(new CreateEdgeCommand(model), HttpContext.RequestAborted);

    [HttpPut("{id:int}"),
     EndpointSummary(nameof(UpdateEdge)),
     EndpointDescription("Изменение кромки"),
     ProducesResponseType(typeof(UpdateEdgeCommandResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Результат изменения")]
    public Task<UpdateEdgeCommandResult> UpdateEdge(
        [FromRoute, Description("Id кромки")] int id,
        [FromBody, Description("Параметры кромки")] UpdateEdgeModel model,
        [FromServices] ICommandHandler<UpdateEdgeCommand, UpdateEdgeCommandResult> handler) =>
        handler.Handle(new UpdateEdgeCommand(id, model), HttpContext.RequestAborted);

    [HttpPost("{id:int}/comments"),
     EndpointSummary(nameof(AddEdgeComment)),
     EndpointDescription("Добавление комментария"),
     ProducesResponseType(typeof(EdgeCommentDto), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Добавленный комментарий")]
    public Task<EdgeCommentDto> AddEdgeComment(
        [FromRoute, Description("Id кромки")] int id,
        [FromBody, Description("Комментарий")] AddEdgeCommentModel model,
        [FromServices] ICommandHandler<AddEdgeCommentCommand, EdgeCommentDto> handler) =>
        handler.Handle(new AddEdgeCommentCommand(id, model), HttpContext.RequestAborted);

    [HttpDelete("{id:int}/comments/{commentId:int}"),
     EndpointSummary(nameof(DeleteEdgeComment)),
     EndpointDescription("Удаление комментария"),
     ProducesResponseType(typeof(DeleteEdgeCommentCommandResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Результат команды")]
    public Task<DeleteEdgeCommentCommandResult> DeleteEdgeComment(
        [FromRoute, Description("Id кромки")] int id,
        [FromRoute, Description("Id комментария")] int commentId,
        [FromServices] ICommandHandler<DeleteEdgeCommentCommand, DeleteEdgeCommentCommandResult> handler) =>
        handler.Handle(new DeleteEdgeCommentCommand(id, commentId), HttpContext.RequestAborted);

    [HttpDelete("{id:int}"),
     EndpointSummary(nameof(DeleteEdge)),
     EndpointDescription("Удаление кромки"),
     ProducesResponseType(typeof(DeleteEdgeCommandResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Результат удаления")]
    public Task<DeleteEdgeCommandResult> DeleteEdge(
        [FromRoute, Description("Id кромки")] int id,
        [FromServices] ICommandHandler<DeleteEdgeCommand, DeleteEdgeCommandResult> handler) =>
        handler.Handle(new DeleteEdgeCommand(id), HttpContext.RequestAborted);

    [HttpPost("images"),
     Consumes("multipart/form-data"),
     SkipRequestBodyLogging,
     RequestSizeLimit(3 * 1024 * 1024),
     EndpointSummary(nameof(UploadEdgeImage)),
     EndpointDescription("Загрузка изображения кромки"),
     ProducesResponseType(typeof(UploadEdgeImageCommandResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Данные загруженного изображения")]
    public Task<UploadEdgeImageCommandResult> UploadEdgeImage(
        [FromForm, Description("Параметры изображения")] UploadEdgeImageCommand command,
        [FromServices] ICommandHandler<UploadEdgeImageCommand, UploadEdgeImageCommandResult> handler) =>
        handler.Handle(command, HttpContext.RequestAborted);

    [HttpPost("{id:int}/stroydvor/sync"),
     EndpointSummary(nameof(SyncEdge)),
     EndpointDescription("Синхронизация кромки со Стройдвором"),
     ProducesResponseType(typeof(EdgeStroydvorSyncDto), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Результат синхронизации")]
    public Task<EdgeStroydvorSyncDto> SyncEdge(
        [FromRoute, Description("Id кромки")] int id,
        [FromServices] ICommandHandler<SyncEdgeStroydvorCommand, EdgeStroydvorSyncDto> handler) =>
        handler.Handle(new SyncEdgeStroydvorCommand(id), HttpContext.RequestAborted);

    [HttpPost("stroydvor/sync"),
     EndpointSummary(nameof(SyncEdges)),
     EndpointDescription("Синхронизация списка кромок со Стройдвором"),
     ProducesResponseType(typeof(SyncEdgesStroydvorCommandResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Результаты синхронизации")]
    public Task<SyncEdgesStroydvorCommandResult> SyncEdges(
        [FromBody, Description("Кромки для синхронизации")] SyncEdgesStroydvorCommand command,
        [FromServices] ICommandHandler<SyncEdgesStroydvorCommand, SyncEdgesStroydvorCommandResult> handler) =>
        handler.Handle(command, HttpContext.RequestAborted);
}