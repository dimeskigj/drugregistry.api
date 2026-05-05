using DrugRegistry.API.Contracts.V2;
using DrugRegistry.API.Endpoints.Interfaces;
using DrugRegistry.API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

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
        var group = app.MapGroup("/api/v2/drugs").WithTags("Drugs V2");

        group.MapGet("/", async (
                IDrugService drugService,
                [FromQuery] int? page,
                [FromQuery] int? size,
                [FromQuery] string? query,
                [FromQuery(Name = "id")] Guid[]? ids) =>
            {
                if (ids is { Length: > 0 })
                {
                    if (!string.IsNullOrWhiteSpace(query))
                        return V2ProblemResponses.BadRequest("The 'query' filter cannot be combined with 'id'.");

                    if (page.HasValue || size.HasValue)
                        return V2ProblemResponses.BadRequest(
                            "Pagination parameters cannot be combined with explicit 'id' filters.");

                    var drugsById = (await drugService.GetDrugsByIds(ids)).Select(d => d.ToResponse()).ToList();
                    return Results.Ok(new PagedResponse<DrugResponse>(drugsById, drugsById.Count, 0, drugsById.Count));
                }

                var pageNumber = page ?? 0;
                var pageSize = size ?? 10;

                if (pageNumber < 0 || pageSize <= 0)
                    return V2ProblemResponses.BadRequest(
                        "'page' must be greater than or equal to 0 and 'size' must be greater than 0.");

                if (!string.IsNullOrWhiteSpace(query))
                {
                    var searched = await drugService.QueryDrugs(query.Trim(), pageNumber, pageSize);
                    return Results.Ok(searched.ToResponse());
                }

                var paged = await drugService.GetDrugsPaginated(pageNumber, pageSize);
                return Results.Ok(paged.ToResponse());
            })
            .Produces<PagedResponse<DrugResponse>>()
            .ProducesProblem(400)
            .WithName("List V2 drugs")
            .WithSummary("List or filter drugs")
            .WithDescription("Returns paged drugs. Supports query filtering or explicit id filtering.");

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
            .WithSummary("Get a drug by id");

        group.MapGet("/ean/{ean}", async (
                IDrugService drugService,
                string ean) =>
            {
                var trimmedEan = ean.Trim();
                if (string.IsNullOrWhiteSpace(trimmedEan))
                    return V2ProblemResponses.BadRequest("The 'ean' path parameter is required.");

                var drug = await drugService.GetDrugByEan(trimmedEan);
                return drug is null
                    ? V2ProblemResponses.NotFound($"Drug with EAN '{trimmedEan}' was not found.")
                    : Results.Ok(drug.ToResponse());
            })
            .Produces<DrugResponse>()
            .ProducesProblem(400)
            .ProducesProblem(404)
            .WithName("Get V2 drug by ean")
            .WithSummary("Get a drug by EAN");

        return app;
    }
}