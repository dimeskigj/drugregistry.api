using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using DrugRegistry.API.Database;
using DrugRegistry.API.Domain;

namespace DrugRegistry.API.Services;

public class CsvEanSeeder(AppDbContext dbContext, IHttpClientFactory httpClientFactory, ILogger<CsvEanSeeder> logger)
{
    public async Task SeedAsync()
    {
        try
        {
            logger.LogInformation("Starting CSV EAN seeding from {Url}", Constants.CsvUrl);
            var client = httpClientFactory.CreateClient();
            var response = await client.GetAsync(Constants.CsvUrl);
            response.EnsureSuccessStatusCode();

            await using var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new StreamReader(stream);
            var csvConfiguration = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                PrepareHeaderForMatch = args => args.Header.Trim().ToLowerInvariant()
            };
            using var csv = new CsvReader(reader, csvConfiguration);

            if (!await csv.ReadAsync()) return;
            csv.ReadHeader();

            var eanDataList = new List<DrugEanData>();
            var seenEans = new HashSet<string>(StringComparer.Ordinal);

            while (await csv.ReadAsync())
            {
                if (!csv.TryGetField("ean_code", out string? eanCode) || string.IsNullOrWhiteSpace(eanCode)) continue;

                eanCode = eanCode.Trim();
                if (!seenEans.Add(eanCode)) continue;

                var eanItem = new DrugEanData
                {
                    EanCode = eanCode,
                    DecisionNumber = GetValue(csv, "solution_number"),
                    LatinName = GetValue(csv, "latin_name"),
                    GenericName = GetValue(csv, "generic_name_multiple"),
                    PharmaceuticalForm = GetValue(csv, "pharmacy_form"),
                    Strength = GetValue(csv, "strength"),
                    Packaging = GetValue(csv, "drug_package")
                };

                eanDataList.Add(eanItem);
            }

            if (eanDataList.Count > 0)
            {
                await dbContext.DrugEanData.AddRangeAsync(eanDataList);
                await dbContext.SaveChangesAsync();
                logger.LogInformation("Successfully seeded {Count} unique EAN records.", eanDataList.Count);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while seeding CSV EAN data.");
        }
    }

    private static string? GetValue(CsvReader csv, string headerName)
    {
        if (!csv.TryGetField(headerName, out string? value)) return null;
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}