using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace DrugRegistry.API.Swagger;

public class V1DeprecatedOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var relativePath = context.ApiDescription.RelativePath;
        if (string.IsNullOrWhiteSpace(relativePath)) return;

        var normalizedPath = relativePath.TrimStart('/');
        if (!normalizedPath.StartsWith("api/", StringComparison.OrdinalIgnoreCase)) return;
        if (normalizedPath.StartsWith("api/v2/", StringComparison.OrdinalIgnoreCase)) return;

        operation.Deprecated = true;
    }
}