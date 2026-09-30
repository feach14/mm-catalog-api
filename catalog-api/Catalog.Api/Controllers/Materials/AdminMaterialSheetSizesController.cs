using Catalog.Api.Features.MaterialSheetSizes;
using Catalog.Api.Features.MaterialSheetSizes.Dto;
using Catalog.Api.Features.MaterialSheetSizes.ForAdmin;
using Core.Attributes;
using Core.Controllers;
using Core.CQRS;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Catalog.Api.Controllers.Materials;

[Route("api/admin/materials/sheet-sizes"),
 OpenApiTagOrder(6),
 Authorize(
    AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme,
    Roles = AppConstants.AdministrationRoles),
 ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, Description = "Источник запроса не прошёл аутентификацию"),
 ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden, Description = "У пользователя нет прав менеджера или администратора")]
public sealed class AdminMaterialSheetSizesController : BaseApiController
{
    [HttpGet,
     EndpointSummary(nameof(MaterialSheetSizes)),
     EndpointDescription("Список размеров материалов"),
     ProducesResponseType(typeof(GetMaterialSheetSizesQueryResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Список размеров материалов")]
    public Task<GetMaterialSheetSizesQueryResult> MaterialSheetSizes(
        [FromServices] IQueryHandler<GetMaterialSheetSizesQuery, GetMaterialSheetSizesQueryResult> handler) =>
        handler.Handle(new GetMaterialSheetSizesQuery(), HttpContext.RequestAborted);

    [HttpGet("{id:int}"),
     EndpointSummary(nameof(MaterialSheetSize)),
     EndpointDescription("Информация о размере материала"),
     ProducesResponseType(typeof(SheetSizeDto), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Размер материала")]
    public Task<SheetSizeDto> MaterialSheetSize(
        [FromRoute, Description("Id размера материала")] int id,
        [FromServices] IQueryHandler<GetMaterialSheetSizeQuery, SheetSizeDto> handler) =>
        handler.Handle(new GetMaterialSheetSizeQuery(id), HttpContext.RequestAborted);

    [HttpPost,
     EndpointSummary(nameof(CreateMaterialSheetSize)),
     EndpointDescription("Создание нового размера материала"),
     ProducesResponseType(typeof(CreateMaterialSheetSizeCommandResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Результат команды")]
    public Task<CreateMaterialSheetSizeCommandResult> CreateMaterialSheetSize(
        [FromBody, Description("Параметры размера материала")] SheetSizeModel model,
        [FromServices] ICommandHandler<CreateMaterialSheetSizeCommand, CreateMaterialSheetSizeCommandResult> handler) =>
        handler.Handle(new CreateMaterialSheetSizeCommand(model), HttpContext.RequestAborted);

    [HttpPut("{id:int}"),
     EndpointSummary(nameof(UpdateMaterialSheetSize)),
     EndpointDescription("Обновление размера материала"),
     ProducesResponseType(typeof(UpdateMaterialSheetSizeCommandResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Результат команды")]
    public Task<UpdateMaterialSheetSizeCommandResult> UpdateMaterialSheetSize(
        [FromRoute, Description("Id размера материала")] int id,
        [FromBody, Description("Параметры размера материала")] SheetSizeModel model,
        [FromServices] ICommandHandler<UpdateMaterialSheetSizeCommand, UpdateMaterialSheetSizeCommandResult> handler) =>
        handler.Handle(new UpdateMaterialSheetSizeCommand(id, model), HttpContext.RequestAborted);

    [HttpDelete("{id:int}"),
     EndpointSummary(nameof(DeleteMaterialSheetSize)),
     EndpointDescription("Удаление размера материала"),
     ProducesResponseType(typeof(DeleteMaterialSheetSizeCommandResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Результат команды")]
    public Task<DeleteMaterialSheetSizeCommandResult> DeleteMaterialSheetSize(
        [FromRoute, Description("Id размера материала")] int id,
        [FromServices] ICommandHandler<DeleteMaterialSheetSizeCommand, DeleteMaterialSheetSizeCommandResult> handler) =>
        handler.Handle(new DeleteMaterialSheetSizeCommand(id), HttpContext.RequestAborted);

    [HttpPost("change-order-col"),
     EndpointSummary(nameof(ChangeOrderCol)),
     EndpointDescription("Изменение порядка сортировки в списке размеров материалов"),
     ProducesResponseType(typeof(ChangeSortingMaterialSheetSizeCommandResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Результат команды")]
    public Task<ChangeSortingMaterialSheetSizeCommandResult> ChangeOrderCol(
        [FromBody, Description("Параметры сортировки")] ChangeSortingMaterialSheetSizeCommand model,
        [FromServices] ICommandHandler<ChangeSortingMaterialSheetSizeCommand, ChangeSortingMaterialSheetSizeCommandResult> handler) =>
        handler.Handle(model, HttpContext.RequestAborted);
}