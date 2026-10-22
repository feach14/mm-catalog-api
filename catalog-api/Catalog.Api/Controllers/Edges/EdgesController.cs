using Catalog.Api.Features.Edges;
using Core.Attributes;
using Core.Controllers;
using Core.CQRS;
using Core.RequestResponseLogger;
using Microsoft.Net.Http.Headers;

namespace Catalog.Api.Controllers.Edges;

[Route("api/edges"),
 OpenApiTagOrder(15),
 AllowAnonymous]
public class EdgesController : BaseApiController
{
    [HttpGet("images/{fileGuid:guid}"),
     SkipResponseBodyLogging,
     EndpointSummary(nameof(EdgeImage)),
     EndpointDescription("Изображение кромки"),
     ProducesResponseType(typeof(Stream), StatusCodes.Status200OK, MediaTypeNames.Image.Png, MediaTypeNames.Image.Jpeg, "image/webp", Description = "Изображение кромки"),
     ProducesResponseType(typeof(void), StatusCodes.Status304NotModified, Description = "Изображение не изменилось"),
     ProducesResponseType(typeof(void), StatusCodes.Status412PreconditionFailed, Description = "Условие If-Match не выполнено")]
    public async Task<IActionResult> EdgeImage(
        [FromRoute, Description("Guid изображения")] Guid fileGuid,
        [FromServices] IQueryHandler<GetEdgeImageQuery, GetEdgeImageQueryResult> handler)
    {
        var image = await handler.Handle(new GetEdgeImageQuery(fileGuid), HttpContext.RequestAborted);
        Response.Headers.CacheControl = $"public, max-age={ImageCacheDurationSeconds}, immutable";
        return new FileContentResult(image.Data, image.ContentType)
        {
            EntityTag = new EntityTagHeaderValue($"\"{fileGuid:D}\"")
        };
    }

    private const int ImageCacheDurationSeconds = 365 * 24 * 60 * 60;
}