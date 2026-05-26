using DrugRegistry.API.Domain;
using DrugRegistry.API.Endpoints;
using DrugRegistry.API.Endpoints.Interfaces;
using DrugRegistry.API.Services;
using DrugRegistry.API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace DrugRegistry.API.Endpoints.V1;

// ReSharper disable once UnusedType.Global
public class DrugEndpoint : IEndpoint
{
    public IServiceCollection RegisterServices(IServiceCollection collection)
    {
        collection.AddScoped<IDrugService, DrugService>();
        return collection;
    }

    public WebApplication MapEndpoints(WebApplication app)
    {
        app.MapGet("/api/drugs", async (
                IDrugService drugService,
                [FromQuery] int? page,
                [FromQuery] int? size) =>
            {
                var pageNumber = page ?? ApiLimits.DefaultPage;
                var pageSize = size ?? ApiLimits.DefaultPageSize;

                if (RequestValidation.ValidatePagination(pageNumber, pageSize) is { } paginationError)
                    return BadRequest(paginationError);

                return Results.Ok(await drugService.GetDrugsPaginated(pageNumber, pageSize));
            })
            .Produces<PagedResult<Drug>>()
            .ProducesProblem(400)
            .WithName("List drugs")
            .WithTags("Drugs")
            .WithMetadata(new ObsoleteAttribute("Deprecated API version. Use /api/v2/drugs."))
            .RequireRateLimiting(ApiLimits.RateLimitPolicies.PublicApi)
            .CacheOutput(ApiLimits.CachePolicies.List);

        app.MapGet("/api/drugs/search", async (
                    IDrugService drugService,
                    [FromQuery] string query,
                    [FromQuery] int? page,
                    [FromQuery] int? size) =>
                {
                    var pageNumber = page ?? ApiLimits.DefaultPage;
                    var pageSize = size ?? ApiLimits.DefaultPageSize;

                    if (RequestValidation.ValidatePagination(pageNumber, pageSize) is { } paginationError)
                        return BadRequest(paginationError);

                    if (RequestValidation.ValidateRequiredQuery(query, out var normalizedQuery) is { } queryError)
                        return BadRequest(queryError);

                    return Results.Ok(await drugService.QueryDrugs(normalizedQuery, pageNumber, pageSize));
                })
            .Produces<PagedResult<Drug>>()
            .WithName("Search drugs")
            .WithTags("Drugs")
            .WithMetadata(new ObsoleteAttribute("Deprecated API version. Use /api/v2/drugs?query=..."))
            .RequireRateLimiting(ApiLimits.RateLimitPolicies.PublicApi)
            .CacheOutput(ApiLimits.CachePolicies.List);

        app.MapPost("/api/drugs/by-ids", async (
                    IDrugService drugService,
                    [FromBody] IEnumerable<Guid> ids) =>
                {
                    var idList = ids.ToArray();
                    if (RequestValidation.ValidateIdFilters(idList.Length) is { } idFilterError)
                        return BadRequest(idFilterError);

                    return Results.Ok(await drugService.GetDrugsByIds(idList));
                })
            .Produces<IEnumerable<Drug>>()
            .WithName("Find drugs by ids")
            .WithTags("Drugs")
            .WithMetadata(new ObsoleteAttribute("Deprecated API version. Use /api/v2/drugs?id=..."))
            .RequireRateLimiting(ApiLimits.RateLimitPolicies.PublicApi);

        app.MapGet("/api/drugs/ean/{ean}", async (
                IDrugService drugService,
                string ean
            ) =>
            {
                if (RequestValidation.ValidateEan(ean, out var trimmedEan) is { } eanError)
                    return BadRequest(eanError);

                var drug = await drugService.GetDrugByEan(trimmedEan);
                return drug is null ? Results.NotFound($"Drug with EAN '{trimmedEan}' was not found.") : Results.Ok(drug);
            })
            .Produces<Drug>()
            .ProducesProblem(400)
            .ProducesProblem(404)
            .WithName("Find drug by EAN")
            .WithTags("Drugs")
            .WithMetadata(new ObsoleteAttribute("Deprecated API version. Use /api/v2/drugs/ean/{ean}."))
            .RequireRateLimiting(ApiLimits.RateLimitPolicies.PublicApi)
            .CacheOutput(ApiLimits.CachePolicies.Detail);

        return app;
    }

    private static IResult BadRequest(string detail)
    {
        return Results.Problem(
            statusCode: StatusCodes.Status400BadRequest,
            title: "Invalid request",
            detail: detail,
            type: "https://httpstatuses.com/400");
    }
}
