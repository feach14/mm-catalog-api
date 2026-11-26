using System.Diagnostics.CodeAnalysis;
using Catalog.Database.Enums;
using Core.BaseEnums;

namespace Catalog.Database.Entities;

[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local", Justification = "Идентификатор заполняется EF Core.")]
public sealed record PricePercentage
{
    public int Id { get; private init; }
    public required string Key { get; init; }
    public required string Name { get; init; }
    public required int Value { get; init; }
    public required PricePercentageTargetEnum AppliesTo { get; init; }
    public required PriceTypeEnum PriceType { get; init; }
    public required string Description { get; init; }
}