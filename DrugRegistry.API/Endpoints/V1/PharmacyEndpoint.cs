using DrugRegistry.API.Domain;
using DrugRegistry.API.Endpoints.Interfaces;
using DrugRegistry.API.Services;
using DrugRegistry.API.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

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
                Results.Ok(await pharmacyService.GetPharmaciesByDistance(
                    new Location { Longitude = lon, Latitude = lat },
                    page ?? 0, size ?? 10,
                    municipality, place)))
            .Produces<PagedResult<Pharmacy>>()
            .WithName("Query pharmacies by location")
            .WithTags("Pharmacies")
            .WithMetadata(new ObsoleteAttribute("Deprecated API version. Use /api/v2/pharmacies?lon=...&lat=..."));

        app.MapGet("/api/pharmacies/search", async (
                    IPharmacyService pharmacyService,
                    [FromQuery] string query,
                    [FromQuery] int? page,
                    [FromQuery] int? size,
                    [FromQuery] string? municipality,
                    [FromQuery] string? place) =>
                Results.Ok(await pharmacyService.GetPharmaciesByQuery(query,
                    page ?? 0, size ?? 10,
                    municipality, place)))
            .Produces<PagedResult<Pharmacy>>()
            .WithName("Query pharmacies by name and address")
            .WithTags("Pharmacies")
            .WithMetadata(new ObsoleteAttribute("Deprecated API version. Use /api/v2/pharmacies?query=..."));

        app.MapGet("/api/pharmacies/municipalities-by-frequency", async (
                IPharmacyService pharmacyService) => Results.Ok(
                await pharmacyService.GetMunicipalitiesOrderedByFrequency()
            ))
            .Produces<IEnumerable<string>>()
            .WithName("Query places by frequency")
            .WithTags("Pharmacies")
            .WithMetadata(new ObsoleteAttribute("Deprecated API version. Use /api/v2/pharmacies/municipalities."));

        app.MapGet("/api/pharmacies/places-by-frequency", async (
                    IPharmacyService pharmacyService,
                    [FromQuery] string municipality) =>
                Results.Ok(
                    await pharmacyService.GetPlacesOrderedByFrequencyForMunicipality(municipality)
                ))
            .Produces<IEnumerable<string>>()
            .WithName("Query municipalities by frequency")
            .WithTags("Pharmacies")
            .WithMetadata(
                new ObsoleteAttribute(
                    "Deprecated API version. Use /api/v2/pharmacies/municipalities/{municipality}/places."));

        app.MapPost("/api/pharmacies/by-ids", async (
                    IPharmacyService pharmacyService,
                    [FromBody] IEnumerable<Guid> ids) =>
                Results.Ok(await pharmacyService.GetPharmaciesByIds(ids)))
            .Produces<IEnumerable<Pharmacy>>()
            .WithName("Find pharmacies by ids")
            .WithTags("Pharmacies")
            .WithMetadata(new ObsoleteAttribute("Deprecated API version. Use /api/v2/pharmacies?id=..."));

        return app;
    }
}