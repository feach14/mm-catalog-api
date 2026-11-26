using Catalog.Database;
using Core.CQRS;

namespace Catalog.Api.Features.PricePercentages;

public sealed record UpdatePricePercentageModel(
    [property: Description("Значение в процентах"), JsonRequired] int Value);

public sealed class UpdatePricePercentageModelValidator : AbstractValidator<UpdatePricePercentageModel>
{
    public UpdatePricePercentageModelValidator(CatalogDbContext dbContext, IHttpContextAccessor httpContextAccessor)
    {
        RuleFor(model => model.Value)
            .Cascade(CascadeMode.Stop)
            .GreaterThanOrEqualTo(0).WithMessage("Процент не может быть отрицательным.")
            .MustAsync(async (_, ct) =>
            {
                var key = httpContextAccessor.HttpContext?.Request.RouteValues["key"] as string;
                return await dbContext.PricePercentages.AsNoTracking().AnyAsync(percentage => percentage.Key == key, ct);
            }).WithMessage("Процент для цен не найден.");
    }
}

public sealed record UpdatePricePercentageCommand(string Key, UpdatePricePercentageModel Model) : ICommand<UpdatePricePercentageCommandResult>;

public sealed record UpdatePricePercentageCommandResult(
    [property: Description("Успех операции")] bool Success);

public sealed class UpdatePricePercentageCommandHandler(CatalogDbContext dbContext)
    : ICommandHandler<UpdatePricePercentageCommand, UpdatePricePercentageCommandResult>
{
    public async Task<UpdatePricePercentageCommandResult> Handle(UpdatePricePercentageCommand command, CancellationToken ct)
    {
        await dbContext.PricePercentages.Where(percentage => percentage.Key == command.Key)
            .ExecuteUpdateAsync(setters => setters.SetProperty(percentage => percentage.Value, command.Model.Value), ct);
        return new UpdatePricePercentageCommandResult(true);
    }
}