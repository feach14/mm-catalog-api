using System.Diagnostics.CodeAnalysis;
using Catalog.Database.Enums;

#pragma warning disable CS8618 // Параметры заполняются на уровне EF 

namespace Catalog.Database.Entities;

[SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Local")]
public sealed record MaterialImage
{
    public int Id { get; private set; }

    public int? MaterialId { get; set; }
    public Material? Material { get; private set; }
    public int? ArchivedMaterialId { get; set; }
    public ArchivedMaterial? ArchivedMaterial { get; private set; }

    public required Guid Guid { get; init; }
    public required string ContentType { get; init; }
    public required byte[] Data { get; init; }
    public required MaterialImageTypeEnum ImageType { get; init; }
}