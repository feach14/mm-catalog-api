namespace Catalog.Api.Features.Exports.Dto;

public sealed record ExcelExportColumnDto(
    [property: Description("Ключ поля")] string Key,
    [property: Description("Номер колонки Excel от 1 до 100")] int Column);

public sealed class ExcelExportColumnDtoValidator : AbstractValidator<ExcelExportColumnDto>
{
    public ExcelExportColumnDtoValidator()
    {
        RuleFor(x => x.Key).NotEmpty().WithMessage("Не указано поле выгрузки.");
        RuleFor(x => x.Column).InclusiveBetween(1, 100).WithMessage("Номер колонки должен быть от 1 до 100.");
    }
}