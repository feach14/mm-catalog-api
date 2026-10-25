using Catalog.Api.Infrastructure.Excel;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Catalog.Api.Extensions;

public static class ExcelExportOpenApiExtensions
{
    public static IServiceCollection AddExcelExportOpenApi(this IServiceCollection services) =>
        services.ConfigureAll<OpenApiOptions>(options => options.AddOperationTransformer((operation, context, ct) =>
        {
            if (context.Description.SupportedResponseTypes.Any(response => response.StatusCode == StatusCodes.Status200OK
                    && response.ApiResponseFormats.Any(format => format.MediaType == ExcelExportFile.ContentType)))
            {
                operation.Responses ??= new OpenApiResponses();
                operation.Responses["200"] = new OpenApiResponse
                {
                    Description = "Файл Excel",
                    Content = new Dictionary<string, OpenApiMediaType>
                    {
                        [ExcelExportFile.ContentType] = new()
                        {
                            Schema = new OpenApiSchema { Type = JsonSchemaType.String, Format = "binary" }
                        }
                    }
                };
            }
            return Task.CompletedTask;
        }));
}