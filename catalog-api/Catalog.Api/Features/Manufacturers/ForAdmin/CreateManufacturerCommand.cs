using Catalog.Database;
using Catalog.Database.Entities;
using Core.CQRS;

namespace Catalog.Api.Features.Manufacturers.ForAdmin;

public sealed record CreateManufacturerCommand(
    [property: Description("Название производителя")] string Name) : ICommand<CreateManufacturerCommandResult>;
public sealed record CreateManufacturerCommandResult([property: Description("Id производителя")] int Id);

public sealed class CreateManufacturerCommandValidator : AbstractValidator<CreateManufacturerCommand>
{
    public CreateManufacturerCommandValidator(CatalogDbContext dbContext)
    {
        RuleFor(x => x.Name)
            .Cascade(CascadeMode.Stop)
            .NotEmpty().WithMessage("Не указано название производителя")
            .MaximumLength(100).WithMessage("Название производителя не должно быть длиннее 100 символов")
            .MustAsync(async (name, ct) =>
                !await dbContext.MaterialManufacturers.AnyAsync(x => x.Name == name.Trim(), ct))
            .WithMessage("Производитель с таким названием уже существует");
    }
}

public sealed class CreateManufacturerCommandHandler(CatalogDbContext dbContext)
    : ICommandHandler<CreateManufacturerCommand, CreateManufacturerCommandResult>
{
    public async Task<CreateManufacturerCommandResult> Handle(CreateManufacturerCommand command, CancellationToken ct)
    {
        var maxOrderByCol = await dbContext.MaterialManufacturers.MaxAsync(x => (int?)x.OrderByCol, ct) ?? 0;
        var manufacturer = new MaterialManufacturer { Name = command.Name.Trim(), OrderByCol = maxOrderByCol + 1 };
        dbContext.MaterialManufacturers.Add(manufacturer);
        await dbContext.SaveChangesAsync(ct);
        return new CreateManufacturerCommandResult(manufacturer.Id);
    }
}