using Catalog.Api.Features.Materials;
using Catalog.Api.Features.Materials.Public;
using Core.Attributes;
using Core.Controllers;
using Core.CQRS;
using Core.RequestResponseLogger;
using Microsoft.Net.Http.Headers;

namespace Catalog.Api.Controllers.Materials;

[Route("api/materials"),
 OpenApiTagOrder(1),
 AllowAnonymous]
public class MaterialsController : BaseApiController
{
    [HttpGet("search"),
     EndpointSummary(nameof(SearchMaterials)),
     EndpointDescription("Публичный постраничный поиск материалов"),
     ProducesResponseType(typeof(SearchMaterialsQueryResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Страница материалов")]
    public Task<SearchMaterialsQueryResult> SearchMaterials(
        [FromQuery] SearchMaterialsQuery query,
        [FromServices] IQueryHandler<SearchMaterialsQuery, SearchMaterialsQueryResult> handler) =>
        handler.Handle(query, HttpContext.RequestAborted);

    [HttpGet("filters"),
     EndpointSummary(nameof(MaterialFilters)),
     EndpointDescription("Связанные фильтры публичного каталога материалов"),
     ProducesResponseType(typeof(GetMaterialFiltersQueryResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Значения фильтров и количества материалов")]
    public Task<GetMaterialFiltersQueryResult> MaterialFilters(
        [FromQuery] GetMaterialFiltersQuery query,
        [FromServices] IQueryHandler<GetMaterialFiltersQuery, GetMaterialFiltersQueryResult> handler) =>
        handler.Handle(query, HttpContext.RequestAborted);

    [HttpGet,
     EndpointSummary(nameof(Materials)),
     EndpointDescription("Полный публичный список материалов без фильтрации и пагинации. Для поиска, фильтрации, пагинации используйте /api/materials/search"),
     ProducesResponseType(typeof(GetAllMaterialsQueryResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Список материалов")]
    public Task<GetAllMaterialsQueryResult> Materials(
        [FromServices] IQueryHandler<GetAllMaterialsQuery, GetAllMaterialsQueryResult> handler) =>
        handler.Handle(new GetAllMaterialsQuery(), HttpContext.RequestAborted);

    [HttpGet("{id:int}"),
     EndpointSummary(nameof(Material)),
     EndpointDescription("Публичная информация о материале"),
     ProducesResponseType(typeof(GetMaterialQueryResult), StatusCodes.Status200OK, MediaTypeNames.Application.Json, Description = "Материал")]
    public Task<GetMaterialQueryResult> Material(
        [FromRoute, Description("Id материала")] int id,
        [FromServices] IQueryHandler<GetMaterialQuery, GetMaterialQueryResult> handler) =>
        handler.Handle(new GetMaterialQuery(id), HttpContext.RequestAborted);

    [HttpGet("images/{fileGuid:guid}"),
     SkipResponseBodyLogging,
     ResponseCache(Duration = ImageCacheDurationSeconds, Location = ResponseCacheLocation.Any),
     EndpointSummary(nameof(MaterialImage)),
     EndpointDescription("Изображение материала"),
     ProducesResponseType(typeof(Stream), StatusCodes.Status200OK, MediaTypeNames.Image.Png, MediaTypeNames.Image.Jpeg, "image/webp", Description = "Изображение материала"),
     ProducesResponseType(typeof(void), StatusCodes.Status304NotModified, Description = "Изображение не изменилось")]
    public async Task<IActionResult> MaterialImage(
        [FromRoute, Description("Id изображения")] Guid fileGuid,
        [FromServices] IQueryHandler<GetMaterialImageQuery, GetMaterialImageQueryResult> handler)
    {
        var image = await handler.Handle(new GetMaterialImageQuery(fileGuid), HttpContext.RequestAborted);
        return new FileContentResult(image.Data, image.ContentType)
        {
            EntityTag = new EntityTagHeaderValue($"\"{fileGuid:D}\"")
        };
    }

    private const int ImageCacheDurationSeconds = 365 * 24 * 60 * 60;
}