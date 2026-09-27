namespace Catalog.Api.Features.Manufacturers.ForAdmin;

using Core.CQRS;
using Database;

public sealed record UpdateManufacturerCommand : ICommand<UpdateManufacturerCommandResult>
{
    [JsonIgnore]
    public int Id { get; init; }

    [Description("Название производителя")]
    public required string Name { get; init; }
}
public sealed record UpdateManufacturerCommandResult([property: Description("Успех операции")] bool Success);

public sealed class UpdateManufacturerCommandValidator : AbstractValidator<UpdateManufacturerCommand>
{
    public UpdateManufacturerCommandValidator(CatalogDbContext dbContext, IHttpContextAccessor httpContextAccessor)
    {
        RuleFor(x => x.Name)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Не указано название производителя")
            .MaximumLength(100).WithMessage("Название производителя не должно быть длиннее 100 символов")
            .MustAsync(async (name, ct) =>
            {
                var routeId = httpContextAccessor.HttpContext?.Request.RouteValues["id"]?.ToString();
                return int.TryParse(routeId, out var id)
                       && !await dbContext.MaterialManufacturers.AnyAsync(
                           x => x.Id != id && x.Name == name.Trim(), ct);
            })
            .WithMessage("Производитель с таким названием уже существует");
    }
}

public sealed class UpdateManufacturerCommandHandler(CatalogDbContext dbContext)
    : ICommandHandler<UpdateManufacturerCommand, UpdateManufacturerCommandResult>
{
    public async Task<UpdateManufacturerCommandResult> Handle(UpdateManufacturerCommand command, CancellationToken ct)
    {
        var manufacturer = await dbContext.MaterialManufacturers.FirstOrDefaultAsync(x => x.Id == command.Id, ct)
            ?? throw new BadHttpRequestException($"Производитель с id={command.Id} не найден.");
        manufacturer.Name = command.Name.Trim();
        await dbContext.SaveChangesAsync(ct);
        return new UpdateManufacturerCommandResult(true);
    }
}
