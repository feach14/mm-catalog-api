using Catalog.Api.Features.Materials;
using Catalog.Api.Features.Materials.ForAdmin;
using Catalog.Api.Features.Materials.ForAdmin.Dto;
using Catalog.Api.Features.Materials.Public;
using Catalog.Api.Features.Materials.Public.Dto;
using Core.Attributes;
using Core.Controllers;
using Core.CQRS;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Catalog.Api.Controllers.Materials;

[Route("api/materials")]
[OpenApiTagOrder(1)]
[Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
[ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, Description = "Источник запроса не прошёл аутентификацию")]
public class MaterialsController : BaseApiController
{
    [HttpGet]
    [AllowAnonymous]
    [EndpointSummary(nameof(Materials))]
    [EndpointDescription("Публичный список материалов")]
    [ProducesResponseType(typeof(GetAllMaterialsQueryResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Материалы и их категории")]
    public Task<GetAllMaterialsQueryResult> Materials(
        [FromQuery] GetAllMaterialsQuery query,
        [FromServices] IQueryHandler<GetAllMaterialsQuery, GetAllMaterialsQueryResult> handler) =>
        handler.Handle(query, HttpContext.RequestAborted);

    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [EndpointSummary(nameof(Material))]
    [EndpointDescription("Публичная информация о материале")]
    [ProducesResponseType(typeof(GetMaterialQueryResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Материал")]
    public Task<GetMaterialQueryResult> Material(
        [FromRoute, Description("Id материала")] int id,
        [FromServices] IQueryHandler<GetMaterialQuery, GetMaterialQueryResult> handler) =>
        handler.Handle(new GetMaterialQuery(id), HttpContext.RequestAborted);

    [HttpGet("admin")]
    [EndpointSummary(nameof(AdminMaterials))]
    [EndpointDescription("Список материалов для администрирования")]
    [ProducesResponseType(typeof(GetAllMaterialsForAdminQueryResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Список материалов")]
    public Task<GetAllMaterialsForAdminQueryResult> AdminMaterials(
        [FromQuery] GetAllMaterialsForAdminQuery query,
        [FromServices] IQueryHandler<GetAllMaterialsForAdminQuery, GetAllMaterialsForAdminQueryResult> handler) =>
        handler.Handle(query, HttpContext.RequestAborted);

    [HttpGet("admin/{id:int}")]
    [EndpointSummary(nameof(AdminMaterial))]
    [EndpointDescription("Информация о материале для администрирования")]
    [ProducesResponseType(typeof(GetMaterialForAdminQueryResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Материал")]
    public Task<GetMaterialForAdminQueryResult> AdminMaterial(
        [FromRoute, Description("Id материала")] int id,
        [FromServices] IQueryHandler<GetMaterialForAdminQuery, GetMaterialForAdminQueryResult> handler) =>
        handler.Handle(new GetMaterialForAdminQuery(id), HttpContext.RequestAborted);

    [HttpPost]
    [EndpointSummary(nameof(CreateMaterial))]
    [EndpointDescription("Создание нового материала")]
    [ProducesResponseType(typeof(CreateMaterialCommandResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Результат команды")]
    public Task<CreateMaterialCommandResult> CreateMaterial(
        [FromBody, Description("Параметры материала")] MaterialModel model,
        [FromServices] ICommandHandler<CreateMaterialCommand, CreateMaterialCommandResult> handler) =>
        handler.Handle(new CreateMaterialCommand(model), HttpContext.RequestAborted);

    [HttpPut("{id:int}")]
    [EndpointSummary(nameof(UpdateMaterial))]
    [EndpointDescription("Обновление материала")]
    [ProducesResponseType(typeof(UpdateMaterialCommandResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Результат команды")]
    public Task<UpdateMaterialCommandResult> UpdateMaterial(
        [FromRoute, Description("Id материала")] int id,
        [FromBody, Description("Параметры материала")] MaterialModel model,
        [FromServices] ICommandHandler<UpdateMaterialCommand, UpdateMaterialCommandResult> handler) =>
        handler.Handle(new UpdateMaterialCommand(id, model), HttpContext.RequestAborted);

    [HttpDelete("{id:int}")]
    [EndpointSummary(nameof(DeleteMaterial))]
    [EndpointDescription("Удаление материала")]
    [ProducesResponseType(typeof(DeleteMaterialCommandResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Результат команды")]
    public Task<DeleteMaterialCommandResult> DeleteMaterial(
        [FromRoute, Description("Id материала")] int id,
        [FromServices] ICommandHandler<DeleteMaterialCommand, DeleteMaterialCommandResult> handler) =>
        handler.Handle(new DeleteMaterialCommand(id), HttpContext.RequestAborted);

    [HttpPost("change-order-col")]
    [EndpointSummary(nameof(ChangeOrderCol))]
    [EndpointDescription("Изменение порядка сортировки материалов в категории")]
    [ProducesResponseType(typeof(ChangeSortingMaterialCommandResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Результат команды")]
    public Task<ChangeSortingMaterialCommandResult> ChangeOrderCol(
        [FromBody, Description("Параметры сортировки")] ChangeSortingMaterialCommand model,
        [FromServices] ICommandHandler<ChangeSortingMaterialCommand, ChangeSortingMaterialCommandResult> handler) =>
        handler.Handle(model, HttpContext.RequestAborted);

    [HttpGet("images/{fileGuid:guid}")]
    [AllowAnonymous]
    [EndpointSummary(nameof(MaterialImage))]
    [EndpointDescription("Изображение материала")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK, Description = "Изображение материала")]
    public async Task<FileContentResult> MaterialImage(
        [FromRoute, Description("Id изображения")] Guid fileGuid,
        [FromServices] IQueryHandler<GetMaterialImageQuery, GetMaterialImageQueryResult> handler)
    {
        var image = await handler.Handle(new GetMaterialImageQuery(fileGuid), HttpContext.RequestAborted);
        return File(image.Data, image.ContentType);
    }
}
