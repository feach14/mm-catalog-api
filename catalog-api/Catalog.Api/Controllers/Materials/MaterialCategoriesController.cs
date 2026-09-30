using Catalog.Api.Features.MaterialCategories;
using Core.Attributes;
using Core.Controllers;
using Core.CQRS;

namespace Catalog.Api.Controllers.Materials;

[Route("api/materials/categories"),
 OpenApiTagOrder(2),
 AllowAnonymous]
public class MaterialCategoriesController : BaseApiController
{
    [HttpGet,
     EndpointSummary(nameof(MaterialCategories)),
     EndpointDescription("Список категорий материалов"),
     ProducesResponseType(typeof(GetAllMaterialCategoriesQueryResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Список категорий")]
    public Task<GetAllMaterialCategoriesQueryResult> MaterialCategories(
        [FromServices] IQueryHandler<GetAllMaterialCategoriesQuery, GetAllMaterialCategoriesQueryResult> handler) =>
        handler.Handle(new GetAllMaterialCategoriesQuery(), HttpContext.RequestAborted);

    [HttpGet("{id:int}"),
     EndpointSummary(nameof(MaterialCategory)),
     EndpointDescription("Информация о категории материалов"),
     ProducesResponseType(typeof(GetMaterialCategoryQueryResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Категория")]
    public Task<GetMaterialCategoryQueryResult> MaterialCategory(
        [FromRoute, Description("Id категории")] int id,
        [FromServices] IQueryHandler<GetMaterialCategoryQuery, GetMaterialCategoryQueryResult> handler) =>
        handler.Handle(new GetMaterialCategoryQuery(id), HttpContext.RequestAborted);
}