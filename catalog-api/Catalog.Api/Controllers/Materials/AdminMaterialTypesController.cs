using Catalog.Api.Features.MaterialTypes.ForAdmin;
using Catalog.Api.Features.MaterialTypes.ForAdmin.Dto;
using Core.Attributes;
using Core.Controllers;
using Core.CQRS;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Catalog.Api.Controllers.Materials;

[Route("api/admin/materials/material-types"),
 OpenApiTagOrder(8),
 Authorize(
    AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme,
    Roles = AppConstants.AdministrationRoles),
 ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, Description = "Источник запроса не прошёл аутентификацию"),
 ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden, Description = "У пользователя нет прав менеджера или администратора")]
public sealed class AdminMaterialTypesController : BaseApiController
{
    [HttpGet,
     EndpointSummary(nameof(MaterialTypes)),
     EndpointDescription("Список типов материалов"),
     ProducesResponseType(typeof(GetMaterialTypesQueryResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Список типов материалов")]
    public Task<GetMaterialTypesQueryResult> MaterialTypes(
        [FromServices] IQueryHandler<GetMaterialTypesQuery, GetMaterialTypesQueryResult> handler) =>
        handler.Handle(new GetMaterialTypesQuery(), HttpContext.RequestAborted);

    [HttpGet("{id:int}"),
     EndpointSummary(nameof(MaterialType)),
     EndpointDescription("Информация о типе материала"),
     ProducesResponseType(typeof(GetMaterialTypeQueryResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Тип материала")]
    public Task<GetMaterialTypeQueryResult> MaterialType(
        [FromRoute, Description("Id типа материала")] int id,
        [FromServices] IQueryHandler<GetMaterialTypeQuery, GetMaterialTypeQueryResult> handler) =>
        handler.Handle(new GetMaterialTypeQuery(id), HttpContext.RequestAborted);

    [HttpPost,
     EndpointSummary(nameof(CreateMaterialType)),
     EndpointDescription("Создание нового типа материала"),
     ProducesResponseType(typeof(CreateMaterialTypeCommandResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Результат команды")]
    public Task<CreateMaterialTypeCommandResult> CreateMaterialType(
        [FromBody, Description("Параметры типа материала")] MaterialTypeModel model,
        [FromServices] ICommandHandler<CreateMaterialTypeCommand, CreateMaterialTypeCommandResult> handler) =>
        handler.Handle(new CreateMaterialTypeCommand(model), HttpContext.RequestAborted);

    [HttpPut("{id:int}"),
     EndpointSummary(nameof(UpdateMaterialType)),
     EndpointDescription("Обновление типа материала"),
     ProducesResponseType(typeof(UpdateMaterialTypeCommandResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Результат команды")]
    public Task<UpdateMaterialTypeCommandResult> UpdateMaterialType(
        [FromRoute, Description("Id типа материала")] int id,
        [FromBody, Description("Параметры типа материала")] MaterialTypeModel model,
        [FromServices] ICommandHandler<UpdateMaterialTypeCommand, UpdateMaterialTypeCommandResult> handler) =>
        handler.Handle(new UpdateMaterialTypeCommand(id, model), HttpContext.RequestAborted);

    [HttpDelete("{id:int}"),
     EndpointSummary(nameof(DeleteMaterialType)),
     EndpointDescription("Удаление типа материала"),
     ProducesResponseType(typeof(DeleteMaterialTypeCommandResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Результат команды")]
    public Task<DeleteMaterialTypeCommandResult> DeleteMaterialType(
        [FromRoute, Description("Id типа материала")] int id,
        [FromServices] ICommandHandler<DeleteMaterialTypeCommand, DeleteMaterialTypeCommandResult> handler) =>
        handler.Handle(new DeleteMaterialTypeCommand(id), HttpContext.RequestAborted);

    [HttpPost("change-order-col"),
     EndpointSummary(nameof(ChangeOrderCol)),
     EndpointDescription("Изменение порядка сортировки в списке типов материалов"),
     ProducesResponseType(typeof(ChangeSortingMaterialTypeCommandResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Результат команды")]
    public Task<ChangeSortingMaterialTypeCommandResult> ChangeOrderCol(
        [FromBody, Description("Параметры сортировки")] ChangeSortingMaterialTypeCommand model,
        [FromServices] ICommandHandler<ChangeSortingMaterialTypeCommand, ChangeSortingMaterialTypeCommandResult> handler) =>
        handler.Handle(model, HttpContext.RequestAborted);
}