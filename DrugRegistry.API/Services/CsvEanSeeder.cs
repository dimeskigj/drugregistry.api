using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using DrugRegistry.API.Database;
using DrugRegistry.API.Domain;
using Microsoft.EntityFrameworkCore;

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
                PrepareHeaderForMatch = args => args.Header.Trim().ToLowerInvariant(),
                IgnoreBlankLines = true,
                BadDataFound = null
            };
            using var csv = new CsvReader(reader, csvConfiguration);
            csv.Context.RegisterClassMap<DrugEanCsvRecordMap>();

            var existingEans = (await dbContext.DrugEanData.AsNoTracking()
                    .Select(e => e.EanCode)
                    .ToListAsync())
                .ToHashSet(StringComparer.Ordinal);

            var eanDataList = new List<DrugEanData>();
            var parsedCount = 0;
            var insertedCount = 0;

            foreach (var record in csv.GetRecords<DrugEanCsvRecord>())
            {
                var eanCode = Normalize(record.EanCode);
                if (string.IsNullOrWhiteSpace(eanCode)) continue;

                parsedCount++;
                if (!existingEans.Add(eanCode)) continue;

                var eanItem = new DrugEanData
                {
                    EanCode = eanCode,
                    DecisionNumber = Normalize(record.DecisionNumber),
                    LatinName = Normalize(record.LatinName),
                    GenericName = Normalize(record.GenericName),
                    PharmaceuticalForm = Normalize(record.PharmaceuticalForm),
                    Strength = Normalize(record.Strength),
                    Packaging = Normalize(record.Packaging)
                };

                eanDataList.Add(eanItem);
                insertedCount++;
            }

            if (eanDataList.Count > 0)
            {
                await dbContext.DrugEanData.AddRangeAsync(eanDataList);
                await dbContext.SaveChangesAsync();
                logger.LogInformation(
                    "Successfully synchronized CSV EAN data. Parsed {ParsedCount} rows and inserted {InsertedCount} new records.",
                    parsedCount,
                    insertedCount);
            }
            else
            {
                logger.LogInformation(
                    "CSV EAN data is already up-to-date. Parsed {ParsedCount} rows and inserted 0 records.",
                    parsedCount);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while seeding CSV EAN data.");
            throw;
        }
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private sealed class DrugEanCsvRecord
    {
        public string? EanCode { get; init; }
        public string? DecisionNumber { get; init; }
        public string? LatinName { get; init; }
        public string? GenericName { get; init; }
        public string? PharmaceuticalForm { get; init; }
        public string? Strength { get; init; }
        public string? Packaging { get; init; }
    }

    private sealed class DrugEanCsvRecordMap : ClassMap<DrugEanCsvRecord>
    {
        public DrugEanCsvRecordMap()
        {
            Map(m => m.EanCode).Name("ean_code");
            Map(m => m.DecisionNumber).Name("solution_number");
            Map(m => m.LatinName).Name("latin_name");
            Map(m => m.GenericName).Name("generic_name_multiple");
            Map(m => m.PharmaceuticalForm).Name("pharmacy_form");
            Map(m => m.Strength).Name("strength");
            Map(m => m.Packaging).Name("drug_package");
        }
    }
}