using Catalog.Api.Features.MaterialCategories;
using Catalog.Api.Features.MaterialCategories.Dto;
using Catalog.Api.Features.MaterialCategories.ForAdmin;
using Catalog.Api.Features.MaterialCategories.ForAdmin.Dto;
using Core.Attributes;
using Core.Controllers;
using Core.CQRS;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Catalog.Api.Controllers.Materials;

[Route("api/materials/categories")]
[OpenApiTagOrder(1)]
[Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
[ProducesResponseType(typeof(void), StatusCodes.Status401Unauthorized, Description = "Источник запроса не прошёл аутентификацию")]
public class MaterialCategoriesController : BaseApiController
{
    [HttpGet]
    [AllowAnonymous]
    [EndpointSummary(nameof(MaterialCategories))]
    [EndpointDescription("Список категорий материалов")]
    [ProducesResponseType(typeof(MaterialCategoryForAdminDto[]), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Список категорий")]
    public Task<GetAllMaterialCategoriesQueryResult> MaterialCategories(
        [FromServices] IQueryHandler<GetAllMaterialCategoriesQuery, GetAllMaterialCategoriesQueryResult> handler) =>
        handler.Handle(new GetAllMaterialCategoriesQuery(), HttpContext.RequestAborted);
    
    [HttpGet("{id:int}")]
    [AllowAnonymous]
    [EndpointSummary(nameof(MaterialCategory))]
    [EndpointDescription("Информация о категории материалов")]
    [ProducesResponseType(typeof(GetMaterialCategoryQueryResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Категория")]
    public Task<GetMaterialCategoryQueryResult> MaterialCategory(
        [FromRoute, Description("Id категории")] int id,
        [FromServices] IQueryHandler<GetMaterialCategoryQuery, GetMaterialCategoryQueryResult> handler) =>
        handler.Handle(new GetMaterialCategoryQuery(id), HttpContext.RequestAborted);

    [HttpPost]
    [EndpointSummary(nameof(CreateMaterialCategory))]
    [EndpointDescription("Создание новой категории материалов")]
    [ProducesResponseType(typeof(CreateMaterialCategoryCommandResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Результат команды")]
    public Task<CreateMaterialCategoryCommandResult> CreateMaterialCategory(
        [FromBody, Description("Параметры категории")] MaterialCategoryModel model,
        [FromServices] ICommandHandler<CreateMaterialCategoryCommand, CreateMaterialCategoryCommandResult> handler) =>
        handler.Handle(new CreateMaterialCategoryCommand(model), HttpContext.RequestAborted);

    [HttpPut("{id:int}")]
    [EndpointSummary(nameof(CreateMaterialCategory))]
    [EndpointDescription("Обновление информации у категории материалов")]
    [ProducesResponseType(typeof(UpdateMaterialCategoryCommandResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Результат команды")]
    public Task<UpdateMaterialCategoryCommandResult> UpdateMaterialCategory(
        [FromRoute, Description("Id категории")] int id, 
        [FromBody, Description("Параметры категории")] MaterialCategoryModel model,
        [FromServices] ICommandHandler<UpdateMaterialCategoryCommand, UpdateMaterialCategoryCommandResult> handler) =>
        handler.Handle(new UpdateMaterialCategoryCommand(id, model), HttpContext.RequestAborted);

    [HttpDelete("{id:int}")]
    [EndpointSummary(nameof(DeleteMaterialCategory))]
    [EndpointDescription("Удаление категории материалов")]
    [ProducesResponseType(typeof(DeleteMaterialCategoryCommandResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Результат команды")]
    public Task<DeleteMaterialCategoryCommandResult> DeleteMaterialCategory(
        [FromRoute, Description("Id категории")] int id,
        [FromServices] ICommandHandler<DeleteMaterialCategoryCommand, DeleteMaterialCategoryCommandResult> handler) =>
        handler.Handle(new DeleteMaterialCategoryCommand(id), HttpContext.RequestAborted);

    [HttpPost("change-order-col")]
    [EndpointSummary(nameof(ChangeOrderCol))]
    [EndpointDescription("Изменение порядка сортировки в списке категорий")]
    [ProducesResponseType(typeof(ChangeSortingMaterialCategoryCommandResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Результат команды")]
    public Task<ChangeSortingMaterialCategoryCommandResult> ChangeOrderCol(
        [FromBody, Description("Параметры сортировки")] ChangeSortingMaterialCategoryCommand model,
        [FromServices] ICommandHandler<ChangeSortingMaterialCategoryCommand, ChangeSortingMaterialCategoryCommandResult> handler) =>
        handler.Handle(model, HttpContext.RequestAborted);
}
