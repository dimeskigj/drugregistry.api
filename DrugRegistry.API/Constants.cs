namespace DrugRegistry.API;

public static class Constants
{
    public const string AppName = "DrugRegistry.API/0.1";

    public const string CsvUrl =
        "https://data.gov.mk/dataset/a930f47e-a059-4cbc-8c0b-83ca48fb234f/resource/ecff2aef-9c8e-4efd-a557-96df4fff9adb/download/lekovi.csv";

    private const string LekoviWeb = "https://lekovi.zdravstvo.gov.mk";
    private const string NominatimGeocodingApi = "https://nominatim.openstreetmap.org";
    public static readonly Uri LekoviWebUrl = new(LekoviWeb);
    public static readonly Uri NominatimGeocodingApiUrl = new(NominatimGeocodingApi);

    public static class Quartz
    {
        public const string DrugScrapingJobName = nameof(DrugScrapingJobName);
        public const string DrugScrapingTriggerName = nameof(DrugScrapingTriggerName);
        public const string PharmacyScrapingJobName = nameof(PharmacyScrapingJobName);
        public const string PharmacyScrapingTriggerName = nameof(PharmacyScrapingTriggerName);
    }
}