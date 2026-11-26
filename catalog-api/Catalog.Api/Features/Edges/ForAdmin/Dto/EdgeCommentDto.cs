namespace Catalog.Api.Features.Edges.ForAdmin.Dto;

public sealed record EdgeCommentDto(
    [property: Description("Id")] int Id,
    [property: Description("Текст комментария")] string Text,
    [property: Description("Комментарий для клиента")] bool IsForClient);