namespace DrugRegistry.API;

public static class ApiLimits
{
    public const int DefaultPage = 0;
    public const int DefaultPageSize = 10;
    public const int MaxPage = 500;
    public const int MaxPageSize = 20;
    public const int MaxIdFilters = 50;
    public const int MinQueryLength = 2;
    public const int MaxQueryLength = 80;
    public const int MaxMunicipalityLength = 100;
    public const int MaxPlaceLength = 100;
    public const int MaxEanLength = 32;

    public static class Coordinates
    {
        public const double MinLongitude = -180;
        public const double MaxLongitude = 180;
        public const double MinLatitude = -90;
        public const double MaxLatitude = 90;
    }

    public static class RateLimitPolicies
    {
        public const string PublicApi = "public-api";
        public const string Docs = "docs";
        public const string Health = "health";
    }

    public static class CachePolicies
    {
        public const string List = "list";
        public const string Detail = "detail";
        public const string Lookup = "lookup";
    }

    public static class Cors
    {
        public const string PolicyName = "configured-origins";
    }
}
