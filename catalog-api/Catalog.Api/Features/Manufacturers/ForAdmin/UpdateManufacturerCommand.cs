using Catalog.Api.Features.History;
using Catalog.Database;
using Catalog.Database.Enums;
using Core.CQRS;

namespace Catalog.Api.Features.Manufacturers.ForAdmin;

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

public sealed class UpdateManufacturerCommandHandler(CatalogDbContext dbContext, ICatalogHistoryWriter historyWriter)
    : ICommandHandler<UpdateManufacturerCommand, UpdateManufacturerCommandResult>
{
    public async Task<UpdateManufacturerCommandResult> Handle(UpdateManufacturerCommand command, CancellationToken ct)
    {
        var manufacturer = await dbContext.MaterialManufacturers.FirstOrDefaultAsync(x => x.Id == command.Id, ct)
            ?? throw new BadHttpRequestException($"Производитель с id={command.Id} не найден.");
        var newName = command.Name.Trim();
        if (manufacturer.Name == newName)
            return new UpdateManufacturerCommandResult(true);
        var oldName = manufacturer.Name;
        manufacturer.Name = newName;
        historyWriter.Add(
            CatalogHistoryActionType.Update,
            CatalogHistoryEntityType.Manufacturer,
            manufacturer.Id,
            $"Производитель #{manufacturer.Id} изменён: название «{oldName}» → «{newName}».");
        await dbContext.SaveChangesAsync(ct);
        return new UpdateManufacturerCommandResult(true);
    }
}