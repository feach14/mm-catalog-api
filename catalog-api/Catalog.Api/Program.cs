using Catalog.Api;
using Catalog.Database;
using Core.AccountAuth;
using Core.Cors;
using Core.Extensions;
using Core.RequestResponseLogger;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<AppSettingsConfig>(builder.Configuration.Bind);
var appSettingsConfig = builder.Configuration.Get<AppSettingsConfig>()
                        ?? throw new InvalidOperationException($"Missing {nameof(AppSettingsConfig)}");

builder.Services
    .AddHttpContextAccessor()
    .AddHttpClient()
    .AddFluentValidation<Program>()
    .AddFeatures(typeof(Program).Assembly)
    .AddDbContextPool<CatalogDbContext>(opt =>
    {
        opt.UseNpgsql(appSettingsConfig.DbConnString);
        opt.UseSnakeCaseNamingConvention();
    })
    .AddDbContextPool<DataProtectionKeyDbContext>(opt =>
    {
        opt.UseNpgsql(appSettingsConfig.DbConnString);
        opt.UseSnakeCaseNamingConvention();
    })
    .AddLogging(loggingBuilder =>
    {
        loggingBuilder.AddSeq(builder.Configuration.GetSection("Seq"));
        if (builder.Environment.IsDevelopment())
            loggingBuilder.AddConsole();
    })
    .AddOpenApiFromConfig<Program>(builder.Configuration)
    .AddDomainNameCorsPolicy(appSettingsConfig.Cors.DomainName)
    .AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.WriteIndented = true);

builder.Services.AddDataProtection()
    .PersistKeysToDbContext<DataProtectionKeyDbContext>()
    .SetApplicationName($"{appSettingsConfig.Cookie.CookieName}({appSettingsConfig.Cookie.DomainName})");

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
        options.Cookie.Name = appSettingsConfig.Cookie.CookieName;
        options.Cookie.Domain = appSettingsConfig.Cookie.DomainName;
    });

builder.Services.AddMemoryCache();

var app = builder.Build();
await app.DataProtectionInit();
app.UseMiddleware<RequestResponseLoggerMiddleware>();
app.MapOpenApi(ApiDocExtensions.OpenApiFilePath);
app.UseDocUi(app.Configuration);

if (app.Environment.IsProduction())
    app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
app.UseDomainNameCorsPolicy(appSettingsConfig.Cors.DomainName);
app.UseProblemDetails();
app.MapControllers();

app.Run();
