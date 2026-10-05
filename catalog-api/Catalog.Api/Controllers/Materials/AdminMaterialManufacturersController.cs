using Catalog.Api.Features.Manufacturers.Dto;
using Catalog.Api.Features.Manufacturers.ForAdmin;
using Core.AccountAuth;
using Core.Attributes;
using Core.Controllers;
using Core.CQRS;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Catalog.Api.Controllers.Materials;

[Route("api/admin/materials/manufacturers"),
 OpenApiTagOrder(5),
 Authorize(
    AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme,
    Roles = AuthConstants.AdministrationRoles),
 ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, Description = "Источник запроса не прошёл аутентификацию"),
 ProducesResponseType(typeof(void), StatusCodes.Status403Forbidden, Description = "У пользователя нет прав менеджера или администратора")]
public sealed class AdminMaterialManufacturersController : BaseApiController
{
    [HttpGet,
     EndpointSummary(nameof(Manufacturers)),
     EndpointDescription("Список производителей"),
     ProducesResponseType(typeof(GetManufacturersQueryResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Список производителей")]
    public Task<GetManufacturersQueryResult> Manufacturers(
        [FromServices] IQueryHandler<GetManufacturersQuery, GetManufacturersQueryResult> handler) =>
        handler.Handle(new GetManufacturersQuery(), HttpContext.RequestAborted);

    [HttpGet("{id:int}"),
     EndpointSummary(nameof(Manufacturer)),
     EndpointDescription("Информация о производителе"),
     ProducesResponseType(typeof(GetManufacturerQueryResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Производитель")]
    public Task<GetManufacturerQueryResult> Manufacturer(
        [FromRoute, Description("Id производителя")] int id,
        [FromServices] IQueryHandler<GetManufacturerQuery, GetManufacturerQueryResult> handler) =>
        handler.Handle(new GetManufacturerQuery(id), HttpContext.RequestAborted);

    [HttpPost,
     EndpointSummary(nameof(CreateManufacturer)),
     EndpointDescription("Создание нового производителя"),
     ProducesResponseType(typeof(CreateManufacturerCommandResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Результат команды")]
    public Task<CreateManufacturerCommandResult> CreateManufacturer(
        [FromBody, Description("Параметры производителя")] MaterialManufacturerModel model,
        [FromServices] ICommandHandler<CreateManufacturerCommand, CreateManufacturerCommandResult> handler) =>
        handler.Handle(new CreateManufacturerCommand(model), HttpContext.RequestAborted);

    [HttpPut("{id:int}"),
     EndpointSummary(nameof(UpdateManufacturer)),
     EndpointDescription("Обновление производителя"),
     ProducesResponseType(typeof(UpdateManufacturerCommandResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Результат команды")]
    public Task<UpdateManufacturerCommandResult> UpdateManufacturer(
        [FromRoute, Description("Id производителя")] int id,
        [FromBody, Description("Параметры производителя")] MaterialManufacturerModel model,
        [FromServices] ICommandHandler<UpdateManufacturerCommand, UpdateManufacturerCommandResult> handler) =>
        handler.Handle(new UpdateManufacturerCommand(id, model), HttpContext.RequestAborted);

    [HttpDelete("{id:int}"),
     EndpointSummary(nameof(DeleteManufacturer)),
     EndpointDescription("Удаление производителя"),
     ProducesResponseType(typeof(DeleteManufacturerCommandResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Результат команды")]
    public Task<DeleteManufacturerCommandResult> DeleteManufacturer(
        [FromRoute, Description("Id производителя")] int id,
        [FromServices] ICommandHandler<DeleteManufacturerCommand, DeleteManufacturerCommandResult> handler) =>
        handler.Handle(new DeleteManufacturerCommand(id), HttpContext.RequestAborted);

    [HttpPost("change-order-col"),
     EndpointSummary(nameof(ChangeOrderCol)),
     EndpointDescription("Изменение порядка сортировки в списке производителей"),
     ProducesResponseType(typeof(ChangeSortingManufacturerCommandResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Результат команды")]
    public Task<ChangeSortingManufacturerCommandResult> ChangeOrderCol(
        [FromBody, Description("Параметры сортировки")] ChangeSortingManufacturerCommand model,
        [FromServices] ICommandHandler<ChangeSortingManufacturerCommand, ChangeSortingManufacturerCommandResult> handler) =>
        handler.Handle(model, HttpContext.RequestAborted);
}