using Core.Configuration;

namespace Catalog.Api;

public sealed record AppSettingsConfig
{
    public required string AppName { get; init; }
    public required CorsConfiguration Cors { get; init; }
    public required CookieConfiguration Cookie { get; init; }
    public required string DbConnString { get; init; }
    public required string DataProtectionDbConnString { get; init; }
    public required CalculateApiConfiguration CalculateApi { get; init; }
    public CatalogAuthenticationConfiguration Authentication { get; init; } = new();
}

public sealed record CatalogAuthenticationConfiguration
{
    public AuthenticationProviderEnum Provider { get; init; } = AuthenticationProviderEnum.Accounts;
}

public enum AuthenticationProviderEnum
{
    Accounts,
    Vek
}

public sealed record CalculateApiConfiguration
{
    public required Uri BaseAddress { get; init; }
    public required string MaterialPricePercentagesPath { get; init; }
    public required string EdgePricePercentagesPath { get; init; }
}