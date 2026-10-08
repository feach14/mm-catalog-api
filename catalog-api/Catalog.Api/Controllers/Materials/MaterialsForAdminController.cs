using Catalog.Api.Features.Materials.ForAdmin;
using Core.AccountAuth;
using Core.Attributes;
using Core.Controllers;
using Core.CQRS;
using Core.RequestResponseLogger;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Catalog.Api.Controllers.Materials;

[Route("api/for-admin/materials"),
 OpenApiTagOrder(2),
 Authorize(
    AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme,
    Roles = AuthConstants.AdministrationRoles),
 ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, Description = "Источник запроса не прошёл аутентификацию"),
 ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden, Description = "У пользователя нет прав менеджера или администратора")]
public class MaterialsForAdminController : BaseApiController
{
    [HttpGet,
     EndpointSummary(nameof(Materials)),
     EndpointDescription("Список материалов для административной панели"),
     ProducesResponseType(typeof(GetAllMaterialsForAdminQueryResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Список материалов")]
    public Task<GetAllMaterialsForAdminQueryResult> Materials(
        [FromServices] IQueryHandler<GetAllMaterialsForAdminQuery, GetAllMaterialsForAdminQueryResult> handler) =>
        handler.Handle(new GetAllMaterialsForAdminQuery(), HttpContext.RequestAborted);

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
     EndpointDescription("Изменение порядка сортировки материалов в категории"),
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