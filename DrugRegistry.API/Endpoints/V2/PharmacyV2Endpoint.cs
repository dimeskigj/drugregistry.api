using DrugRegistry.API.Contracts.V2;
using DrugRegistry.API.Domain;
using DrugRegistry.API.Endpoints.Interfaces;
using DrugRegistry.API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace DrugRegistry.API.Endpoints.V2;

// ReSharper disable once UnusedType.Global
public class PharmacyV2Endpoint : IEndpoint
{
    public IServiceCollection RegisterServices(IServiceCollection collection)
    {
        return collection;
    }

    public WebApplication MapEndpoints(WebApplication app)
    {
        var group = app.MapGroup("/api/v2/pharmacies").WithTags("Pharmacies V2");

        group.MapGet("/", async (
                IPharmacyService pharmacyService,
                [FromQuery] int? page,
                [FromQuery] int? size,
                [FromQuery] string? query,
                [FromQuery] double? lon,
                [FromQuery] double? lat,
                [FromQuery] string? municipality,
                [FromQuery] string? place,
                [FromQuery(Name = "id")] Guid[]? ids) =>
            {
                if (ids is { Length: > 0 })
                {
                    if (!string.IsNullOrWhiteSpace(query) || lon.HasValue || lat.HasValue ||
                        !string.IsNullOrWhiteSpace(municipality) || !string.IsNullOrWhiteSpace(place))
                        return V2ProblemResponses.BadRequest(
                            "The 'id' filter cannot be combined with query, location, municipality, or place filters.");

                    if (page.HasValue || size.HasValue)
                        return V2ProblemResponses.BadRequest(
                            "Pagination parameters cannot be combined with explicit 'id' filters.");

                    var pharmaciesById = (await pharmacyService.GetPharmaciesByIds(ids)).Select(p => p.ToResponse())
                        .ToList();

                    return Results.Ok(
                        new PagedResponse<PharmacyResponse>(pharmaciesById, pharmaciesById.Count, 0,
                            pharmaciesById.Count));
                }

                var pageNumber = page ?? 0;
                var pageSize = size ?? 10;

                if (pageNumber < 0 || pageSize <= 0)
                    return V2ProblemResponses.BadRequest(
                        "'page' must be greater than or equal to 0 and 'size' must be greater than 0.");

                var hasLongitude = lon.HasValue;
                var hasLatitude = lat.HasValue;
                if (hasLongitude != hasLatitude)
                    return V2ProblemResponses.BadRequest("Both 'lon' and 'lat' must be provided together.");

                if (hasLongitude && !string.IsNullOrWhiteSpace(query))
                    return V2ProblemResponses.BadRequest(
                        "The 'query' filter cannot be combined with geographic distance sorting ('lon' and 'lat').");

                if (!string.IsNullOrWhiteSpace(query))
                {
                    var searched = await pharmacyService.GetPharmaciesByQuery(query.Trim(), pageNumber, pageSize,
                        municipality, place);
                    return Results.Ok(searched.ToResponse());
                }

                if (hasLongitude && hasLatitude)
                {
                    var byDistance = await pharmacyService.GetPharmaciesByDistance(
                        new Location { Longitude = lon!.Value, Latitude = lat!.Value },
                        pageNumber,
                        pageSize,
                        municipality,
                        place);
                    return Results.Ok(byDistance.ToResponse());
                }

                var paged = await pharmacyService.GetPharmaciesPaginated(pageNumber, pageSize, municipality, place);
                return Results.Ok(paged.ToResponse());
            })
            .Produces<PagedResponse<PharmacyResponse>>()
            .ProducesProblem(400)
            .WithName("List V2 pharmacies")
            .WithSummary("List or filter pharmacies")
            .WithDescription(
                "Returns paged pharmacies. Supports search by query, ordering by distance, filtering by municipality/place, or explicit id filtering.");

        group.MapGet("/{id:guid}", async (
                IPharmacyService pharmacyService,
                Guid id) =>
            {
                var pharmacy = await pharmacyService.GetPharmacyById(id);
                return pharmacy is null
                    ? V2ProblemResponses.NotFound($"Pharmacy with id '{id}' was not found.")
                    : Results.Ok(pharmacy.ToResponse());
            })
            .Produces<PharmacyResponse>()
            .ProducesProblem(404)
            .WithName("Get V2 pharmacy by id")
            .WithSummary("Get a pharmacy by id");

        group.MapGet("/municipalities", async (
                    IPharmacyService pharmacyService) =>
                Results.Ok(await pharmacyService.GetMunicipalitiesOrderedByFrequency()))
            .Produces<IEnumerable<string>>()
            .WithName("List V2 municipalities by pharmacy frequency")
            .WithSummary("List municipalities by pharmacy frequency");

        group.MapGet("/municipalities/{municipality}/places", async (
                IPharmacyService pharmacyService,
                string municipality) =>
            {
                if (string.IsNullOrWhiteSpace(municipality))
                    return V2ProblemResponses.BadRequest("The 'municipality' path parameter is required.");

                var places = await pharmacyService.GetPlacesOrderedByFrequencyForMunicipality(municipality.Trim());
                return Results.Ok(places);
            })
            .Produces<IEnumerable<string>>()
            .ProducesProblem(400)
            .WithName("List V2 municipality places by pharmacy frequency")
            .WithSummary("List places in a municipality by pharmacy frequency");

        return app;
    }
}