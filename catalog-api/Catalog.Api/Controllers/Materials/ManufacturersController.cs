namespace Catalog.Api.Controllers.Materials;

using Core.Attributes;
using Core.Controllers;
using Core.CQRS;
using Features.Manufacturers;
using Features.Manufacturers.Dto;
using Features.Manufacturers.ForAdmin;
using Microsoft.AspNetCore.Authentication.Cookies;

[Route("api/materials/manufacturers")]
[OpenApiTagOrder(4)]
[Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
[ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, Description = "Источник запроса не прошёл аутентификацию")]
public sealed class ManufacturersController : BaseApiController
{
    [HttpGet, AllowAnonymous]
    [EndpointSummary(nameof(Manufacturers))]
    [EndpointDescription("Список производителей")]
    [ProducesResponseType(typeof(ManufacturerDto[]), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Список производителей")]
    public Task<GetManufacturersQueryResult> Manufacturers(
        [FromServices] IQueryHandler<GetManufacturersQuery, GetManufacturersQueryResult> handler) =>
        handler.Handle(new GetManufacturersQuery(), HttpContext.RequestAborted);

    [HttpGet("{id:int}"), AllowAnonymous]
    [EndpointSummary(nameof(Manufacturer))]
    [EndpointDescription("Информация о производителе")]
    [ProducesResponseType(typeof(ManufacturerDto), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Производитель")]
    public Task<ManufacturerDto> Manufacturer(
        [FromRoute, Description("Id производителя")] int id,
        [FromServices] IQueryHandler<GetManufacturerQuery, ManufacturerDto> handler) =>
        handler.Handle(new GetManufacturerQuery(id), HttpContext.RequestAborted);

    [HttpPost]
    [EndpointSummary(nameof(CreateManufacturer))]
    [EndpointDescription("Создание нового производителя")]
    [ProducesResponseType(typeof(CreateManufacturerCommandResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Результат команды")]
    public Task<CreateManufacturerCommandResult> CreateManufacturer(
        [FromBody, Description("Параметры производителя")] CreateManufacturerCommand command,
        [FromServices] ICommandHandler<CreateManufacturerCommand, CreateManufacturerCommandResult> handler) =>
        handler.Handle(command, HttpContext.RequestAborted);

    [HttpPut("{id:int}")]
    [EndpointSummary(nameof(UpdateManufacturer))]
    [EndpointDescription("Обновление производителя")]
    [ProducesResponseType(typeof(UpdateManufacturerCommandResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Результат команды")]
    public Task<UpdateManufacturerCommandResult> UpdateManufacturer(
        [FromRoute, Description("Id производителя")] int id,
        [FromBody, Description("Параметры производителя")] UpdateManufacturerCommand command,
        [FromServices] ICommandHandler<UpdateManufacturerCommand, UpdateManufacturerCommandResult> handler) =>
        handler.Handle(command with { Id = id }, HttpContext.RequestAborted);

    [HttpDelete("{id:int}")]
    [EndpointSummary(nameof(DeleteManufacturer))]
    [EndpointDescription("Удаление производителя")]
    [ProducesResponseType(typeof(DeleteManufacturerCommandResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Результат команды")]
    public Task<DeleteManufacturerCommandResult> DeleteManufacturer(
        [FromRoute, Description("Id производителя")] int id,
        [FromServices] ICommandHandler<DeleteManufacturerCommand, DeleteManufacturerCommandResult> handler) =>
        handler.Handle(new DeleteManufacturerCommand(id), HttpContext.RequestAborted);

    [HttpPost("change-order-col")]
    [EndpointSummary(nameof(ChangeOrderCol))]
    [EndpointDescription("Изменение порядка сортировки в списке производителей")]
    [ProducesResponseType(typeof(ChangeSortingManufacturerCommandResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Результат команды")]
    public Task<ChangeSortingManufacturerCommandResult> ChangeOrderCol(
        [FromBody, Description("Параметры сортировки")] ChangeSortingManufacturerCommand model,
        [FromServices] ICommandHandler<ChangeSortingManufacturerCommand, ChangeSortingManufacturerCommandResult> handler) =>
        handler.Handle(model, HttpContext.RequestAborted);
}
