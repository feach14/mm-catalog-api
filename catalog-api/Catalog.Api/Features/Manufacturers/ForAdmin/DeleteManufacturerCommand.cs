using Catalog.Database;
using Core.CQRS;

namespace Catalog.Api.Features.Manufacturers.ForAdmin;

public sealed record DeleteManufacturerCommand(int Id) : ICommand<DeleteManufacturerCommandResult>;
public sealed record DeleteManufacturerCommandResult([property: Description("Успех операции")] bool Success);

public sealed class DeleteManufacturerCommandHandler(CatalogDbContext dbContext)
    : ICommandHandler<DeleteManufacturerCommand, DeleteManufacturerCommandResult>
{
    public async Task<DeleteManufacturerCommandResult> Handle(DeleteManufacturerCommand command, CancellationToken ct)
    {
        var manufacturer = await dbContext.MaterialManufacturers.FirstOrDefaultAsync(x => x.Id == command.Id, ct)
            ?? throw new BadHttpRequestException($"Производитель с id={command.Id} не найден.");
        if (await dbContext.Materials.AnyAsync(x => x.MaterialManufacturerId == command.Id, ct))
            throw new BadHttpRequestException("Нельзя удалить производителя, который используется материалами.");

        dbContext.MaterialManufacturers.Remove(manufacturer);
        await dbContext.SaveChangesAsync(ct);
        return new DeleteManufacturerCommandResult(true);
    }
}