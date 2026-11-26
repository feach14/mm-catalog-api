using Catalog.Api.Features.Edges.ForAdmin.Dto;
using Catalog.Api.Infrastructure.Ordering;
using Catalog.Api.Services.History;
using Catalog.Database;
using Catalog.Database.Entities;
using Catalog.Database.Enums;
using Core.CQRS;

namespace Catalog.Api.Features.Edges.ForAdmin;

public sealed record AddEdgeCommentModel(
    [property: Description("Текст комментария, не более 50 символов")] string Text,
    [property: Description("Комментарий для клиента")] bool IsForClient = false);

public sealed class AddEdgeCommentModelValidator : AbstractValidator<AddEdgeCommentModel>
{
    public AddEdgeCommentModelValidator()
    {
        RuleFor(model => model.Text)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Введите комментарий.")
            .MaximumLength(50).WithMessage("Комментарий должен содержать не более 50 символов.");
    }
}

public sealed record AddEdgeCommentCommand(int Id, AddEdgeCommentModel Model) : ICommand<EdgeCommentDto>;

public sealed class AddEdgeCommentCommandHandler(CatalogDbContext dbContext, ICatalogHistoryWriter historyWriter)
    : ICommandHandler<AddEdgeCommentCommand, EdgeCommentDto>
{
    public async Task<EdgeCommentDto> Handle(AddEdgeCommentCommand command, CancellationToken ct)
    {
        return await CatalogOrderTransaction.Run(dbContext, async () =>
        {
            var edge = await dbContext.Edges.AsNoTracking()
                .Where(edge => edge.Id == command.Id)
                .Select(edge => new { edge.Id, edge.Name })
                .SingleOrDefaultAsync(ct);
            if (edge is null)
                throw new BadHttpRequestException("Кромка не найдена.");

            var comment = new EdgeComment
            {
                EdgeId = edge.Id,
                Text = command.Model.Text.Trim(),
                IsForClient = command.Model.IsForClient
            };
            dbContext.EdgeComments.Add(comment);
            historyWriter.Add(CatalogHistoryActionTypeEnum.Update, CatalogHistoryEntityTypeEnum.Edge, command.Id,
                $"Кромка #{command.Id} «{edge.Name}»: добавлен комментарий{(comment.IsForClient ? " для клиента" : string.Empty)} «{comment.Text}».");
            await dbContext.SaveChangesAsync(ct);
            return new EdgeCommentDto(comment.Id, comment.Text, comment.IsForClient);
        }, ct);
    }
}