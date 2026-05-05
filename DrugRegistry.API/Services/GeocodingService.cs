using DrugRegistry.API.Domain;
using DrugRegistry.API.Domain.Scraping;
using DrugRegistry.API.Services.Interfaces;

namespace DrugRegistry.API.Services;

/// <summary>
///     https://operations.osmfoundation.org/policies/nominatim/
///     No heavy uses (an absolute maximum of 1 request per second).
///     Provide a valid HTTP Referer or User-Agent identifying the application (stock User-Agents as set by http libraries
///     will not do).
///     Clearly display attribution as suitable for your medium.
///     Data is provided under the ODbL license which requires to share alike (although small extractions are likely to be
///     covered by fair usage / fair dealing).
/// </summary>
public class GeocodingService : BaseHttpService, IGeocodingService
{
    public GeocodingService(IHttpClientFactory httpClientFactory) : base(httpClientFactory)
    {
        Client.DefaultRequestHeaders.Add("User-Agent", Constants.AppName);
    }

    public async Task<Location?> GeocodePlace(string query, int millisecondsDelay = 0)
    {
        var uri = new Uri(Constants.NominatimGeocodingApiUrl,
            $"https://nominatim.openstreetmap.org/search?q={query}&format=json&limit=1"
        );

        await Task.Delay(millisecondsDelay);

        var result = await Get<List<NominatimResponseDto>>(uri);

        if (!result?.Any() ?? false) return null;

        return new Location { Latitude = result!.First().Lat, Longitude = result!.First().Lon };
    }
}