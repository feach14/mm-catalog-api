#pragma warning disable CS8618 // Параметры заполняются на уровне EF 

using System.Diagnostics.CodeAnalysis;

namespace Catalog.Database.Entities;

[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
public sealed record MaterialImage
{
    public int Id { get; private set; }
    
    public required int MaterialId { get; init; }
    public Material Material { get; private set; }

    public required Guid Guid { get; init; }
    public required string Type { get; init; }
    public required byte[] Data { get; init; }
}