using DrugRegistry.API.Domain;
using DrugRegistry.API.Endpoints.Interfaces;
using DrugRegistry.API.Services;
using DrugRegistry.API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace DrugRegistry.API.Endpoints;

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
                var pageNumber = page ?? 0;
                var pageSize = size ?? 10;

                if (pageNumber < 0 || pageSize < 0)
                    return Results.BadRequest("Page and size parameters must be non-negative.");

                return Results.Ok(await drugService.GetDrugsPaginated(pageNumber, pageSize));
            })
            .Produces<PagedResult<Drug>>()
            .ProducesProblem(400)
            .WithName("List drugs")
            .WithTags("Drugs");

        app.MapGet("/api/drugs/search", async (
                    IDrugService drugService,
                    [FromQuery] string query,
                    [FromQuery] int? page,
                    [FromQuery] int? size) =>
                Results.Ok(await drugService.QueryDrugs(query, page ?? 0, size ?? 10)))
            .Produces<PagedResult<Drug>>()
            .WithName("Search drugs")
            .WithTags("Drugs");

        app.MapPost("/api/drugs/by-ids", async (
                    IDrugService drugService,
                    [FromBody] IEnumerable<Guid> ids) =>
                Results.Ok(await drugService.GetDrugsByIds(ids)))
            .Produces<IEnumerable<Drug>>()
            .WithName("Find drugs by ids")
            .WithTags("Drugs");

        app.MapGet("/api/drugs/ean/{ean}", async (
                IDrugService drugService,
                string ean
            ) =>
            {
                var drug = await drugService.GetDrugByEan(ean.Trim());
                return drug is null ? Results.NotFound($"Drug with EAN '{ean}' was not found.") : Results.Ok(drug);
            })
            .Produces<Drug>()
            .ProducesProblem(404)
            .WithName("Find drug by EAN")
            .WithTags("Drugs");

        return app;
    }
}