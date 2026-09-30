using Catalog.Api.Features.MaterialThicknesses;
using Catalog.Api.Features.MaterialThicknesses.ForAdmin;
using Catalog.Api.Features.MaterialThicknesses.ForAdmin.Dto;
using Core.Attributes;
using Core.Controllers;
using Core.CQRS;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Catalog.Api.Controllers.Materials;

[Route("api/admin/materials/thicknesses"),
 OpenApiTagOrder(7),
 Authorize(
    AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme,
    Roles = AppConstants.AdministrationRoles),
 ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, Description = "Источник запроса не прошёл аутентификацию"),
 ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden, Description = "У пользователя нет прав менеджера или администратора")]
public sealed class AdminMaterialThicknessesController : BaseApiController
{
    [HttpGet,
     EndpointSummary(nameof(MaterialThicknesses)),
     EndpointDescription("Список толщин материалов"),
     ProducesResponseType(typeof(GetMaterialThicknessesQueryResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Список толщин материалов")]
    public Task<GetMaterialThicknessesQueryResult> MaterialThicknesses(
        [FromServices] IQueryHandler<GetMaterialThicknessesQuery, GetMaterialThicknessesQueryResult> handler) =>
        handler.Handle(new GetMaterialThicknessesQuery(), HttpContext.RequestAborted);

    [HttpGet("{id:int}"),
     EndpointSummary(nameof(MaterialThickness)),
     EndpointDescription("Информация о толщине материала"),
     ProducesResponseType(typeof(GetMaterialThicknessQueryResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Толщина материала")]
    public Task<GetMaterialThicknessQueryResult> MaterialThickness(
        [FromRoute, Description("Id толщины материала")] int id,
        [FromServices] IQueryHandler<GetMaterialThicknessQuery, GetMaterialThicknessQueryResult> handler) =>
        handler.Handle(new GetMaterialThicknessQuery(id), HttpContext.RequestAborted);

    [HttpPost,
     EndpointSummary(nameof(CreateMaterialThickness)),
     EndpointDescription("Создание новой толщины материала"),
     ProducesResponseType(typeof(CreateMaterialThicknessCommandResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Результат команды")]
    public Task<CreateMaterialThicknessCommandResult> CreateMaterialThickness(
        [FromBody, Description("Параметры толщины материала")] MaterialThicknessModel model,
        [FromServices] ICommandHandler<CreateMaterialThicknessCommand, CreateMaterialThicknessCommandResult> handler) =>
        handler.Handle(new CreateMaterialThicknessCommand(model), HttpContext.RequestAborted);

    [HttpPut("{id:int}"),
     EndpointSummary(nameof(UpdateMaterialThickness)),
     EndpointDescription("Обновление толщины материала"),
     ProducesResponseType(typeof(UpdateMaterialThicknessCommandResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Результат команды")]
    public Task<UpdateMaterialThicknessCommandResult> UpdateMaterialThickness(
        [FromRoute, Description("Id толщины материала")] int id,
        [FromBody, Description("Параметры толщины материала")] UpdateMaterialThicknessModel model,
        [FromServices] ICommandHandler<UpdateMaterialThicknessCommand, UpdateMaterialThicknessCommandResult> handler) =>
        handler.Handle(new UpdateMaterialThicknessCommand(id, model), HttpContext.RequestAborted);

    [HttpDelete("{id:int}"),
     EndpointSummary(nameof(DeleteMaterialThickness)),
     EndpointDescription("Удаление толщины материала"),
     ProducesResponseType(typeof(DeleteMaterialThicknessCommandResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Результат команды")]
    public Task<DeleteMaterialThicknessCommandResult> DeleteMaterialThickness(
        [FromRoute, Description("Id толщины материала")] int id,
        [FromServices] ICommandHandler<DeleteMaterialThicknessCommand, DeleteMaterialThicknessCommandResult> handler) =>
        handler.Handle(new DeleteMaterialThicknessCommand(id), HttpContext.RequestAborted);
}