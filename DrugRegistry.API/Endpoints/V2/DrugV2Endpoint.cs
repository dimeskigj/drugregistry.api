using DrugRegistry.API.Endpoints;
using DrugRegistry.API.Contracts.V2;
using DrugRegistry.API.Endpoints.Interfaces;
using DrugRegistry.API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace DrugRegistry.API.Endpoints.V2;

// ReSharper disable once UnusedType.Global
public class DrugV2Endpoint : IEndpoint
{
    public IServiceCollection RegisterServices(IServiceCollection collection)
    {
        return collection;
    }

    public WebApplication MapEndpoints(WebApplication app)
    {
        var group = app.MapGroup("/api/v2/drugs")
            .WithTags("Drugs V2")
            .RequireRateLimiting(ApiLimits.RateLimitPolicies.PublicApi);

        group.MapGet("/", async (
                IDrugService drugService,
                [FromQuery] int? page,
                [FromQuery] int? size,
                [FromQuery] string? query,
                [FromQuery(Name = "id")] Guid[]? ids) =>
            {
                if (ids is { Length: > 0 })
                {
                    if (RequestValidation.ValidateIdFilters(ids.Length) is { } idFilterError)
                        return V2ProblemResponses.BadRequest(idFilterError);

                    if (!string.IsNullOrWhiteSpace(query))
                        return V2ProblemResponses.BadRequest("The 'query' filter cannot be combined with 'id'.");

                    if (page.HasValue || size.HasValue)
                        return V2ProblemResponses.BadRequest(
                            "Pagination parameters cannot be combined with explicit 'id' filters.");

                    var drugsById = (await drugService.GetDrugsByIds(ids)).Select(d => d.ToResponse()).ToList();
                    return Results.Ok(new PagedResponse<DrugResponse>(drugsById, drugsById.Count, 0, drugsById.Count));
                }

                var pageNumber = page ?? ApiLimits.DefaultPage;
                var pageSize = size ?? ApiLimits.DefaultPageSize;

                if (RequestValidation.ValidatePagination(pageNumber, pageSize) is { } paginationError)
                    return V2ProblemResponses.BadRequest(paginationError);

                if (RequestValidation.ValidateOptionalQuery(query, out var normalizedQuery) is { } queryError)
                    return V2ProblemResponses.BadRequest(queryError);

                if (normalizedQuery is not null)
                {
                    var searched = await drugService.QueryDrugs(normalizedQuery, pageNumber, pageSize);
                    return Results.Ok(searched.ToResponse());
                }

                var paged = await drugService.GetDrugsPaginated(pageNumber, pageSize);
                return Results.Ok(paged.ToResponse());
            })
            .Produces<PagedResponse<DrugResponse>>()
            .ProducesProblem(400)
            .WithName("List V2 drugs")
            .WithSummary("List or filter drugs")
            .WithDescription(
                "Returns paged drugs. Limits: page 0-500, size 1-20, query 2-80 chars, up to 50 repeated id filters.")
            .CacheOutput(ApiLimits.CachePolicies.List);

        group.MapGet("/{id:guid}", async (
                IDrugService drugService,
                Guid id) =>
            {
                var drug = await drugService.GetDrugById(id);
                return drug is null
                    ? V2ProblemResponses.NotFound($"Drug with id '{id}' was not found.")
                    : Results.Ok(drug.ToResponse());
            })
            .Produces<DrugResponse>()
            .ProducesProblem(404)
            .WithName("Get V2 drug by id")
            .WithSummary("Get a drug by id")
            .CacheOutput(ApiLimits.CachePolicies.Detail);

        group.MapGet("/ean/{ean}", async (
                IDrugService drugService,
                string ean) =>
            {
                if (RequestValidation.ValidateEan(ean, out var trimmedEan) is { } eanError)
                    return V2ProblemResponses.BadRequest(eanError);

                var drug = await drugService.GetDrugByEan(trimmedEan);
                return drug is null
                    ? V2ProblemResponses.NotFound($"Drug with EAN '{trimmedEan}' was not found.")
                    : Results.Ok(drug.ToResponse());
            })
            .Produces<DrugResponse>()
            .ProducesProblem(400)
            .ProducesProblem(404)
            .WithName("Get V2 drug by ean")
            .WithSummary("Get a drug by EAN")
            .CacheOutput(ApiLimits.CachePolicies.Detail);

        return app;
    }
}
