using Core.Configuration;

namespace Catalog.Api;

public sealed record AppSettingsConfig
{
    public required string AppName { get; init; }
    public required CorsConfiguration Cors {get; init; }
    public required CookieConfiguration Cookie {get; init; }
    public required string DbConnString { get; init; }
}
