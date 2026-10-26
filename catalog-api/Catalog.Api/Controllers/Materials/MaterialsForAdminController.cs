using System.Text.Encodings.Web;
using System.Text.Json;
using Catalog.Api.Features.Exports.Dto;
using Catalog.Api.Features.Materials.ForAdmin;
using Catalog.Api.Infrastructure.Excel;
using Core.Attributes;
using Core.Controllers;
using Core.CQRS;
using Core.RequestResponseLogger;

namespace Catalog.Api.Controllers.Materials;

[Route("api/for-admin/materials"),
 OpenApiTagOrder(2)]
public class MaterialsForAdminController : BaseAdministrationApiController
{
    [HttpGet("export/columns"),
     EndpointSummary(nameof(ExportColumns)),
     EndpointDescription("Поля выгрузки материалов в Excel"),
     ProducesResponseType(typeof(ExcelExportColumnsResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Поля выгрузки")]
    public Task<ExcelExportColumnsResult> ExportColumns(
        [FromServices] IQueryHandler<GetMaterialExportColumnsQuery, ExcelExportColumnsResult> handler) =>
        handler.Handle(new GetMaterialExportColumnsQuery(), HttpContext.RequestAborted);

    [HttpPost("export"),
     SkipResponseBodyLogging,
     EndpointSummary(nameof(Export)),
     EndpointDescription("Выгрузка материалов в Excel"),
     ProducesResponseType(typeof(byte[]), StatusCodes.Status200OK, ExcelExportFile.ContentType, Description = "Файл Excel")]
    public async Task<IActionResult> Export(
        [FromBody, Description("Параметры выгрузки")] ExportMaterialsForAdminQuery query,
        [FromServices] IQueryHandler<ExportMaterialsForAdminQuery, ExcelExportFile> handler)
    {
        var result = await handler.Handle(query, HttpContext.RequestAborted);
        Response.Headers.CacheControl = "no-store";
        return File(result.Content, ExcelExportFile.ContentType, result.FileName);
    }

    [HttpGet,
     EndpointSummary(nameof(Materials)),
     EndpointDescription("Список материалов для административной панели"),
     ProducesResponseType(typeof(GetAllMaterialsForAdminQueryResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Список материалов")]
    public Task<GetAllMaterialsForAdminQueryResult> Materials(
        [FromServices] IQueryHandler<GetAllMaterialsForAdminQuery, GetAllMaterialsForAdminQueryResult> handler) =>
        handler.Handle(new GetAllMaterialsForAdminQuery(), HttpContext.RequestAborted);

    [HttpGet("compact"),
     EndpointSummary(nameof(CompactMaterials)),
     EndpointDescription("Компактный список материалов для административной панели"),
     ProducesResponseType(typeof(GetCompactMaterialsForAdminQueryResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Компактный список материалов")]
    public async Task<JsonResult> CompactMaterials(
        [FromServices] IQueryHandler<GetCompactMaterialsForAdminQuery, GetCompactMaterialsForAdminQueryResult> handler,
        [FromServices] IOptions<JsonOptions> jsonOptions)
    {
        var result = await handler.Handle(new GetCompactMaterialsForAdminQuery(), HttpContext.RequestAborted);
        return new JsonResult(result, new JsonSerializerOptions(jsonOptions.Value.JsonSerializerOptions)
        {
            WriteIndented = false,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        });
    }

    [HttpGet("{id:int}"),
     EndpointSummary(nameof(Material)),
     EndpointDescription("Информация о материале для административной панели"),
     ProducesResponseType(typeof(GetMaterialForAdminQueryResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Материал")]
    public Task<GetMaterialForAdminQueryResult> Material(
        [FromRoute, Description("Id материала")] int id,
        [FromServices] IQueryHandler<GetMaterialForAdminQuery, GetMaterialForAdminQueryResult> handler) =>
        handler.Handle(new GetMaterialForAdminQuery(id), HttpContext.RequestAborted);

    [HttpPost,
     EndpointSummary(nameof(CreateMaterial)),
     EndpointDescription("Создание нового материала"),
     ProducesResponseType(typeof(CreateMaterialCommandResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Результат команды")]
    public Task<CreateMaterialCommandResult> CreateMaterial(
        [FromBody, Description("Параметры материала")] CreateMaterialModel model,
        [FromServices] ICommandHandler<CreateMaterialCommand, CreateMaterialCommandResult> handler) =>
        handler.Handle(new CreateMaterialCommand(model), HttpContext.RequestAborted);

    [HttpPut("{id:int}"),
     EndpointSummary(nameof(UpdateMaterial)),
     EndpointDescription("Обновление материала"),
     ProducesResponseType(typeof(UpdateMaterialCommandResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Результат команды")]
    public Task<UpdateMaterialCommandResult> UpdateMaterial(
        [FromRoute, Description("Id материала")] int id,
        [FromBody, Description("Параметры материала")] UpdateMaterialModel model,
        [FromServices] ICommandHandler<UpdateMaterialCommand, UpdateMaterialCommandResult> handler) =>
        handler.Handle(new UpdateMaterialCommand(id, model), HttpContext.RequestAborted);

    [HttpPut("{id:int}/external-link"),
     EndpointSummary(nameof(UpdateMaterialExternalLink)),
     EndpointDescription("Изменение внешней ссылки материала"),
     ProducesResponseType(typeof(UpdateMaterialExternalLinkCommandResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Результат команды")]
    public Task<UpdateMaterialExternalLinkCommandResult> UpdateMaterialExternalLink(
        [FromRoute, Description("Id материала")] int id,
        [FromBody, Description("Внешняя ссылка")] UpdateMaterialExternalLinkModel model,
        [FromServices] ICommandHandler<UpdateMaterialExternalLinkCommand, UpdateMaterialExternalLinkCommandResult> handler) =>
        handler.Handle(new UpdateMaterialExternalLinkCommand(id, model), HttpContext.RequestAborted);

    [HttpPost("{id:int}/clone"),
     EndpointSummary(nameof(CloneMaterial)),
     EndpointDescription("Копирование материала"),
     ProducesResponseType(typeof(CloneMaterialCommandResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Id копии материала")]
    public Task<CloneMaterialCommandResult> CloneMaterial(
        [FromRoute, Description("Id материала")] int id,
        [FromServices] ICommandHandler<CloneMaterialCommand, CloneMaterialCommandResult> handler) =>
        handler.Handle(new CloneMaterialCommand(id), HttpContext.RequestAborted);

    [HttpDelete("{id:int}"),
     EndpointSummary(nameof(DeleteMaterial)),
     EndpointDescription("Удаление материала"),
     ProducesResponseType(typeof(DeleteMaterialCommandResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Результат команды")]
    public Task<DeleteMaterialCommandResult> DeleteMaterial(
        [FromRoute, Description("Id материала")] int id,
        [FromServices] ICommandHandler<DeleteMaterialCommand, DeleteMaterialCommandResult> handler) =>
        handler.Handle(new DeleteMaterialCommand(id), HttpContext.RequestAborted);

    [HttpPost("change-order-col"),
     EndpointSummary(nameof(ChangeOrderCol)),
     EndpointDescription("Изменение общего порядка материалов"),
     ProducesResponseType(typeof(ChangeSortingMaterialCommandResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Результат команды")]
    public Task<ChangeSortingMaterialCommandResult> ChangeOrderCol(
        [FromBody, Description("Параметры сортировки")] ChangeSortingMaterialCommand model,
        [FromServices] ICommandHandler<ChangeSortingMaterialCommand, ChangeSortingMaterialCommandResult> handler) =>
        handler.Handle(model, HttpContext.RequestAborted);

    [HttpPost("images"),
     Consumes("multipart/form-data"),
     SkipRequestBodyLogging,
     RequestSizeLimit(MaxImageRequestSize),
     EndpointSummary(nameof(UploadMaterialImage)),
     EndpointDescription("Загрузка оригинала или готовой миниатюры материала в формате PNG, JPEG или WebP размером до 5 МБ"),
     ProducesResponseType(typeof(UploadMaterialImageCommandResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Данные загруженного изображения")]
    public async Task<UploadMaterialImageCommandResult> UploadMaterialImage(
        [FromForm, Description("Параметры загрузки изображения")] UploadMaterialImageCommand command,
        [FromServices] ICommandHandler<UploadMaterialImageCommand, UploadMaterialImageCommandResult> handler)
        => await handler.Handle(command, HttpContext.RequestAborted);

    private const long MaxImageRequestSize = 6 * 1024 * 1024;
}