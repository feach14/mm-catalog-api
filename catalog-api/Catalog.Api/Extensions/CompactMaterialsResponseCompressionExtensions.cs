using Microsoft.AspNetCore.ResponseCompression;

namespace Catalog.Api.Extensions;

internal static class CompactMaterialsResponseCompressionExtensions
{
    public static IServiceCollection AddCompactMaterialsResponseCompression(this IServiceCollection services)
    {
        return services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
            options.MimeTypes = ["application/json"];
            options.Providers.Add<BrotliCompressionProvider>();
            options.Providers.Add<GzipCompressionProvider>();
        });
    }

    public static IApplicationBuilder UseCompactMaterialsResponseCompression(this IApplicationBuilder app)
    {
        return app.UseWhen(
            context => HttpMethods.IsGet(context.Request.Method)
                       && context.Request.Path == "/api/for-admin/materials/compact",
            branch => branch.UseResponseCompression());
    }
}