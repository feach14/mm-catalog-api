namespace Catalog.Api.Controllers.Materials;

using Core.Attributes;
using Core.Controllers;
using Core.CQRS;
using Features.MaterialSheetSizes;
using Features.MaterialSheetSizes.Dto;
using Features.MaterialSheetSizes.ForAdmin;
using Microsoft.AspNetCore.Authentication.Cookies;

[Route("api/materials/sheet-sizes")]
[OpenApiTagOrder(3)]
[Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
[ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, Description = "Источник запроса не прошёл аутентификацию")]
public sealed class MaterialSheetSizesController : BaseApiController
{
    [HttpGet]
    [AllowAnonymous]
    [EndpointSummary(nameof(MaterialSheetSizes))]
    [EndpointDescription("Список размеров материалов")]
    [ProducesResponseType(typeof(SheetSizeDto[]), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Список размеров материалов")]
    public Task<GetMaterialSheetSizesQueryResult> MaterialSheetSizes(
        [FromServices] IQueryHandler<GetMaterialSheetSizesQuery, GetMaterialSheetSizesQueryResult> handler) =>
        handler.Handle(new GetMaterialSheetSizesQuery(), HttpContext.RequestAborted);

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [EndpointSummary(nameof(MaterialSheetSize))]
    [EndpointDescription("Информация о размере материала")]
    [ProducesResponseType(typeof(SheetSizeDto), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Размер материала")]
    public Task<SheetSizeDto> MaterialSheetSize(
        [FromRoute, Description("Id размера материала")] int id,
        [FromServices] IQueryHandler<GetMaterialSheetSizeQuery, SheetSizeDto> handler) =>
        handler.Handle(new GetMaterialSheetSizeQuery(id), HttpContext.RequestAborted);

    [HttpPost]
    [EndpointSummary(nameof(CreateMaterialSheetSize))]
    [EndpointDescription("Создание нового размера материала")]
    [ProducesResponseType(typeof(CreateMaterialSheetSizeCommandResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Результат команды")]
    public Task<CreateMaterialSheetSizeCommandResult> CreateMaterialSheetSize(
        [FromBody, Description("Параметры размера материала")] SheetSizeModel model,
        [FromServices] ICommandHandler<CreateMaterialSheetSizeCommand, CreateMaterialSheetSizeCommandResult> handler) =>
        handler.Handle(new CreateMaterialSheetSizeCommand(model), HttpContext.RequestAborted);

    [HttpPut("{id:int}")]
    [EndpointSummary(nameof(UpdateMaterialSheetSize))]
    [EndpointDescription("Обновление размера материала")]
    [ProducesResponseType(typeof(UpdateMaterialSheetSizeCommandResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Результат команды")]
    public Task<UpdateMaterialSheetSizeCommandResult> UpdateMaterialSheetSize(
        [FromRoute, Description("Id размера материала")] int id,
        [FromBody, Description("Параметры размера материала")] SheetSizeModel model,
        [FromServices] ICommandHandler<UpdateMaterialSheetSizeCommand, UpdateMaterialSheetSizeCommandResult> handler) =>
        handler.Handle(new UpdateMaterialSheetSizeCommand(id, model), HttpContext.RequestAborted);

    [HttpDelete("{id:int}")]
    [EndpointSummary(nameof(DeleteMaterialSheetSize))]
    [EndpointDescription("Удаление размера материала")]
    [ProducesResponseType(typeof(DeleteMaterialSheetSizeCommandResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Результат команды")]
    public Task<DeleteMaterialSheetSizeCommandResult> DeleteMaterialSheetSize(
        [FromRoute, Description("Id размера материала")] int id,
        [FromServices] ICommandHandler<DeleteMaterialSheetSizeCommand, DeleteMaterialSheetSizeCommandResult> handler) =>
        handler.Handle(new DeleteMaterialSheetSizeCommand(id), HttpContext.RequestAborted);

    [HttpPost("change-order-col")]
    [EndpointSummary(nameof(ChangeOrderCol))]
    [EndpointDescription("Изменение порядка сортировки в списке размеров материалов")]
    [ProducesResponseType(typeof(ChangeSortingMaterialSheetSizeCommandResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Результат команды")]
    public Task<ChangeSortingMaterialSheetSizeCommandResult> ChangeOrderCol(
        [FromBody, Description("Параметры сортировки")] ChangeSortingMaterialSheetSizeCommand model,
        [FromServices] ICommandHandler<ChangeSortingMaterialSheetSizeCommand, ChangeSortingMaterialSheetSizeCommandResult> handler) =>
        handler.Handle(model, HttpContext.RequestAborted);
}
