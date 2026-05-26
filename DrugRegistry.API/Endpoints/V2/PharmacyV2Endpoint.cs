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
        var group = app.MapGroup("/api/v2/pharmacies")
            .WithTags("Pharmacies V2")
            .RequireRateLimiting(ApiLimits.RateLimitPolicies.PublicApi);

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
                    if (RequestValidation.ValidateIdFilters(ids.Length) is { } idFilterError)
                        return V2ProblemResponses.BadRequest(idFilterError);

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

                var pageNumber = page ?? ApiLimits.DefaultPage;
                var pageSize = size ?? ApiLimits.DefaultPageSize;

                if (RequestValidation.ValidatePagination(pageNumber, pageSize) is { } paginationError)
                    return V2ProblemResponses.BadRequest(paginationError);

                if (RequestValidation.ValidateOptionalQuery(query, out var normalizedQuery) is { } queryError)
                    return V2ProblemResponses.BadRequest(queryError);

                if (RequestValidation.ValidateOptionalMunicipality(municipality, out var normalizedMunicipality) is
                    { } municipalityError)
                    return V2ProblemResponses.BadRequest(municipalityError);

                if (RequestValidation.ValidateOptionalPlace(place, out var normalizedPlace) is { } placeError)
                    return V2ProblemResponses.BadRequest(placeError);

                var hasLongitude = lon.HasValue;
                var hasLatitude = lat.HasValue;
                if (hasLongitude != hasLatitude)
                    return V2ProblemResponses.BadRequest("Both 'lon' and 'lat' must be provided together.");

                if (hasLongitude && normalizedQuery is not null)
                    return V2ProblemResponses.BadRequest(
                        "The 'query' filter cannot be combined with geographic distance sorting ('lon' and 'lat').");

                if (hasLongitude && hasLatitude &&
                    RequestValidation.ValidateCoordinates(lon!.Value, lat!.Value) is { } coordinateError)
                    return V2ProblemResponses.BadRequest(coordinateError);

                if (normalizedQuery is not null)
                {
                    var searched = await pharmacyService.GetPharmaciesByQuery(normalizedQuery, pageNumber, pageSize,
                        normalizedMunicipality, normalizedPlace);
                    return Results.Ok(searched.ToResponse());
                }

                if (hasLongitude && hasLatitude)
                {
                    var byDistance = await pharmacyService.GetPharmaciesByDistance(
                        new Location { Longitude = lon!.Value, Latitude = lat!.Value },
                        pageNumber,
                        pageSize,
                        normalizedMunicipality,
                        normalizedPlace);
                    return Results.Ok(byDistance.ToResponse());
                }

                var paged = await pharmacyService.GetPharmaciesPaginated(pageNumber, pageSize, normalizedMunicipality,
                    normalizedPlace);
                return Results.Ok(paged.ToResponse());
            })
            .Produces<PagedResponse<PharmacyResponse>>()
            .ProducesProblem(400)
            .WithName("List V2 pharmacies")
            .WithSummary("List or filter pharmacies")
            .WithDescription(
                "Returns paged pharmacies. Limits: page 0-500, size 1-20, query 2-80 chars, municipality/place up to 100 chars, lon -180..180, lat -90..90, up to 50 repeated id filters.")
            .CacheOutput(ApiLimits.CachePolicies.List);

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
            .WithSummary("Get a pharmacy by id")
            .CacheOutput(ApiLimits.CachePolicies.Detail);

        group.MapGet("/municipalities", async (
                    IPharmacyService pharmacyService) =>
                Results.Ok(await pharmacyService.GetMunicipalitiesOrderedByFrequency()))
            .Produces<IEnumerable<string>>()
            .WithName("List V2 municipalities by pharmacy frequency")
            .WithSummary("List municipalities by pharmacy frequency")
            .CacheOutput(ApiLimits.CachePolicies.Lookup);

        group.MapGet("/municipalities/{municipality}/places", async (
                IPharmacyService pharmacyService,
                string municipality) =>
            {
                if (RequestValidation.ValidateRequiredMunicipality(municipality, out var normalizedMunicipality) is
                    { } municipalityError)
                    return V2ProblemResponses.BadRequest(municipalityError);

                var places = await pharmacyService.GetPlacesOrderedByFrequencyForMunicipality(normalizedMunicipality);
                return Results.Ok(places);
            })
            .Produces<IEnumerable<string>>()
            .ProducesProblem(400)
            .WithName("List V2 municipality places by pharmacy frequency")
            .WithSummary("List places in a municipality by pharmacy frequency")
            .CacheOutput(ApiLimits.CachePolicies.Lookup);

        return app;
    }
}