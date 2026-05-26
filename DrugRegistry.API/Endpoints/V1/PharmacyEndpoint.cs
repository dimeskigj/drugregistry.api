using DrugRegistry.API.Domain;
using DrugRegistry.API.Endpoints.Interfaces;
using DrugRegistry.API.Services;
using DrugRegistry.API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;

namespace DrugRegistry.API.Endpoints.V1;

// ReSharper disable once UnusedType.Global
public class PharmacyEndpoint : IEndpoint
{
    public IServiceCollection RegisterServices(IServiceCollection collection)
    {
        collection.AddScoped<IPharmacyService, PharmacyService>();
        return collection;
    }

    public WebApplication MapEndpoints(WebApplication app)
    {
        app.MapGet("/api/pharmacies/by-location", async (
                    IPharmacyService pharmacyService,
                    [FromQuery] double lon,
                    [FromQuery] double lat,
                    [FromQuery] int? page,
                    [FromQuery] int? size,
                    [FromQuery] string? municipality,
                    [FromQuery] string? place) =>
                {
                    var pageNumber = page ?? ApiLimits.DefaultPage;
                    var pageSize = size ?? ApiLimits.DefaultPageSize;

                    if (RequestValidation.ValidatePagination(pageNumber, pageSize) is { } paginationError)
                        return BadRequest(paginationError);

                    if (RequestValidation.ValidateCoordinates(lon, lat) is { } coordinateError)
                        return BadRequest(coordinateError);

                    if (RequestValidation.ValidateOptionalMunicipality(municipality, out var normalizedMunicipality) is
                        { } municipalityError)
                        return BadRequest(municipalityError);

                    if (RequestValidation.ValidateOptionalPlace(place, out var normalizedPlace) is { } placeError)
                        return BadRequest(placeError);

                    return Results.Ok(await pharmacyService.GetPharmaciesByDistance(
                        new Location { Longitude = lon, Latitude = lat },
                        pageNumber,
                        pageSize,
                        normalizedMunicipality,
                        normalizedPlace));
                })
            .Produces<PagedResult<Pharmacy>>()
            .WithName("Query pharmacies by location")
            .WithTags("Pharmacies")
            .WithMetadata(new ObsoleteAttribute("Deprecated API version. Use /api/v2/pharmacies?lon=...&lat=..."))
            .RequireRateLimiting(ApiLimits.RateLimitPolicies.PublicApi)
            .CacheOutput(ApiLimits.CachePolicies.List);

        app.MapGet("/api/pharmacies/search", async (
                    IPharmacyService pharmacyService,
                    [FromQuery] string query,
                    [FromQuery] int? page,
                    [FromQuery] int? size,
                    [FromQuery] string? municipality,
                    [FromQuery] string? place) =>
                {
                    var pageNumber = page ?? ApiLimits.DefaultPage;
                    var pageSize = size ?? ApiLimits.DefaultPageSize;

                    if (RequestValidation.ValidatePagination(pageNumber, pageSize) is { } paginationError)
                        return BadRequest(paginationError);

                    if (RequestValidation.ValidateRequiredQuery(query, out var normalizedQuery) is { } queryError)
                        return BadRequest(queryError);

                    if (RequestValidation.ValidateOptionalMunicipality(municipality, out var normalizedMunicipality) is
                        { } municipalityError)
                        return BadRequest(municipalityError);

                    if (RequestValidation.ValidateOptionalPlace(place, out var normalizedPlace) is { } placeError)
                        return BadRequest(placeError);

                    return Results.Ok(await pharmacyService.GetPharmaciesByQuery(normalizedQuery,
                        pageNumber, pageSize,
                        normalizedMunicipality, normalizedPlace));
                })
            .Produces<PagedResult<Pharmacy>>()
            .WithName("Query pharmacies by name and address")
            .WithTags("Pharmacies")
            .WithMetadata(new ObsoleteAttribute("Deprecated API version. Use /api/v2/pharmacies?query=..."))
            .RequireRateLimiting(ApiLimits.RateLimitPolicies.PublicApi)
            .CacheOutput(ApiLimits.CachePolicies.List);

        app.MapGet("/api/pharmacies/municipalities-by-frequency", async (
                    IPharmacyService pharmacyService) => Results.Ok(
                    await pharmacyService.GetMunicipalitiesOrderedByFrequency()
                ))
            .Produces<IEnumerable<string>>()
            .WithName("Query places by frequency")
            .WithTags("Pharmacies")
            .WithMetadata(new ObsoleteAttribute("Deprecated API version. Use /api/v2/pharmacies/municipalities."))
            .RequireRateLimiting(ApiLimits.RateLimitPolicies.PublicApi)
            .CacheOutput(ApiLimits.CachePolicies.Lookup);

        app.MapGet("/api/pharmacies/places-by-frequency", async (
                    IPharmacyService pharmacyService,
                    [FromQuery] string municipality) =>
                {
                    if (RequestValidation.ValidateOptionalMunicipality(municipality, out var normalizedMunicipality) is
                        { } municipalityError)
                        return BadRequest(municipalityError);

                    if (normalizedMunicipality is null)
                        return BadRequest("The 'municipality' query parameter is required.");

                    return Results.Ok(
                        await pharmacyService.GetPlacesOrderedByFrequencyForMunicipality(normalizedMunicipality)
                    );
                })
            .Produces<IEnumerable<string>>()
            .WithName("Query municipalities by frequency")
            .WithTags("Pharmacies")
            .WithMetadata(
                new ObsoleteAttribute(
                    "Deprecated API version. Use /api/v2/pharmacies/municipalities/{municipality}/places."))
            .RequireRateLimiting(ApiLimits.RateLimitPolicies.PublicApi)
            .CacheOutput(ApiLimits.CachePolicies.Lookup);

        app.MapPost("/api/pharmacies/by-ids", async (
                    IPharmacyService pharmacyService,
                    [FromBody] IEnumerable<Guid> ids) =>
                {
                    var idList = ids.ToArray();
                    if (RequestValidation.ValidateIdFilters(idList.Length) is { } idFilterError)
                        return BadRequest(idFilterError);

                    return Results.Ok(await pharmacyService.GetPharmaciesByIds(idList));
                })
            .Produces<IEnumerable<Pharmacy>>()
            .WithName("Find pharmacies by ids")
            .WithTags("Pharmacies")
            .WithMetadata(new ObsoleteAttribute("Deprecated API version. Use /api/v2/pharmacies?id=..."))
            .RequireRateLimiting(ApiLimits.RateLimitPolicies.PublicApi);

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
