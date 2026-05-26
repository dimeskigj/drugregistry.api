namespace DrugRegistry.API.Endpoints;

internal static class RequestValidation
{
    public static string? ValidatePagination(int page, int size)
    {
        if (page is < ApiLimits.DefaultPage or > ApiLimits.MaxPage)
            return $"'page' must be between {ApiLimits.DefaultPage} and {ApiLimits.MaxPage}.";

        if (size is < 1 or > ApiLimits.MaxPageSize)
            return $"'size' must be between 1 and {ApiLimits.MaxPageSize}.";

        return null;
    }

    public static string? ValidateIdFilters(int count)
    {
        return count > ApiLimits.MaxIdFilters
            ? $"'id' can be repeated at most {ApiLimits.MaxIdFilters} times."
            : null;
    }

    public static string? ValidateOptionalQuery(string? query, out string? normalizedQuery)
    {
        normalizedQuery = NormalizeOptional(query);
        return normalizedQuery is null ? null : ValidateQueryLength(normalizedQuery, "query");
    }

    public static string? ValidateRequiredQuery(string? query, out string normalizedQuery)
    {
        normalizedQuery = query?.Trim() ?? string.Empty;
        return ValidateQueryLength(normalizedQuery, "query");
    }

    public static string? ValidateOptionalMunicipality(string? municipality, out string? normalizedMunicipality)
    {
        normalizedMunicipality = NormalizeOptional(municipality);
        return ValidateOptionalTextLength(normalizedMunicipality, "municipality", ApiLimits.MaxMunicipalityLength);
    }

    public static string? ValidateOptionalPlace(string? place, out string? normalizedPlace)
    {
        normalizedPlace = NormalizeOptional(place);
        return ValidateOptionalTextLength(normalizedPlace, "place", ApiLimits.MaxPlaceLength);
    }

    public static string? ValidateRequiredMunicipality(string? municipality, out string normalizedMunicipality)
    {
        normalizedMunicipality = municipality?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalizedMunicipality))
            return "The 'municipality' path parameter is required.";

        return normalizedMunicipality.Length > ApiLimits.MaxMunicipalityLength
            ? $"'municipality' must be at most {ApiLimits.MaxMunicipalityLength} characters."
            : null;
    }

    public static string? ValidateEan(string? ean, out string normalizedEan)
    {
        normalizedEan = ean?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalizedEan))
            return "The 'ean' path parameter is required.";

        return normalizedEan.Length > ApiLimits.MaxEanLength
            ? $"'ean' must be at most {ApiLimits.MaxEanLength} characters."
            : null;
    }

    public static string? ValidateCoordinates(double lon, double lat)
    {
        if (!double.IsFinite(lon))
            return "'lon' must be a finite number.";

        if (!double.IsFinite(lat))
            return "'lat' must be a finite number.";

        if (lon is < ApiLimits.Coordinates.MinLongitude or > ApiLimits.Coordinates.MaxLongitude)
            return $"'lon' must be between {ApiLimits.Coordinates.MinLongitude} and {ApiLimits.Coordinates.MaxLongitude}.";

        return lat is < ApiLimits.Coordinates.MinLatitude or > ApiLimits.Coordinates.MaxLatitude
            ? $"'lat' must be between {ApiLimits.Coordinates.MinLatitude} and {ApiLimits.Coordinates.MaxLatitude}."
            : null;
    }

    private static string? ValidateQueryLength(string query, string parameterName)
    {
        return query.Length is < ApiLimits.MinQueryLength or > ApiLimits.MaxQueryLength
            ? $"'{parameterName}' must be between {ApiLimits.MinQueryLength} and {ApiLimits.MaxQueryLength} characters."
            : null;
    }

    private static string? ValidateOptionalTextLength(string? value, string parameterName, int maxLength)
    {
        return value is not null && value.Length > maxLength
            ? $"'{parameterName}' must be at most {maxLength} characters."
            : null;
    }

    private static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
