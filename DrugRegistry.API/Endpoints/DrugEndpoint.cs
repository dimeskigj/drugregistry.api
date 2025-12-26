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
                Results.Ok(await drugService.GetDrugsPaginated(page ?? 0, size ?? 10)))
            .Produces<PagedResult<Drug>>()
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

        return app;
    }
}