using DrugRegistry.API.Database;
using DrugRegistry.API.Domain;
using DrugRegistry.API.Services.Interfaces;
using DrugRegistry.API.Utils;
using FuzzySharp;
using FuzzySharp.SimilarityRatio;
using FuzzySharp.SimilarityRatio.Scorer.StrategySensitive;
using Microsoft.EntityFrameworkCore;

namespace DrugRegistry.API.Services;

public class DrugService(AppDbContext appDbContext) : BaseDbService(appDbContext), IDrugService
{
    private const int MaxItemsPerPage = 20;

    public async Task<List<Drug>> GetAllDrugs()
    {
        return await AppDbContext.Drugs.ToListAsync();
    }

    public async Task<Drug?> GetDrugById(Guid id)
    {
        return await AppDbContext.Drugs.FirstOrDefaultAsync(d => d.Id == id);
    }

    public async Task<Drug?> GetDrugByDecisionNumberAndAtc(string decisionNumber, string atc)
    {
        return await AppDbContext.Drugs.FirstOrDefaultAsync(d => d.DecisionNumber == decisionNumber && d.Atc == atc);
    }

    public async Task<Drug?> GetDrugByUrl(Uri uri)
    {
        return await AppDbContext.Drugs.FirstOrDefaultAsync(d => d.Url == uri);
    }

    public async Task<Guid> AddDrug(Drug drug)
    {
        var res = await AppDbContext.AddAsync(drug);
        await AppDbContext.SaveChangesAsync();
        return res.Entity.Id;
    }

    public async Task<Guid> UpdateDrug(Drug drug, Guid id)
    {
        var existingDrug = await GetDrugById(id);

        if (existingDrug is null) throw new ArgumentException("Invalid drug id");

        existingDrug.DecisionNumber = drug.DecisionNumber;
        existingDrug.Atc = drug.Atc;
        existingDrug.LatinName = drug.LatinName;
        existingDrug.GenericName = drug.GenericName;
        existingDrug.IssuingType = drug.IssuingType;
        existingDrug.Ingredients = drug.Ingredients;
        existingDrug.Strength = drug.Strength;
        existingDrug.Packaging = drug.Packaging;
        existingDrug.PharmaceuticalForm = drug.PharmaceuticalForm;
        existingDrug.Url = drug.Url;
        existingDrug.ManualUrl = drug.ManualUrl;
        existingDrug.ReportUrl = drug.ReportUrl;
        existingDrug.DecisionDate = drug.DecisionDate;
        existingDrug.ValidityDate = drug.ValidityDate;
        existingDrug.ApprovalCarrier = drug.ApprovalCarrier;
        existingDrug.Manufacturer = drug.Manufacturer;
        existingDrug.PriceWithoutVat = drug.PriceWithoutVat;
        existingDrug.PriceWithVat = drug.PriceWithVat;
        existingDrug.LastUpdate = drug.LastUpdate;

        await AppDbContext.SaveChangesAsync();

        return existingDrug.Id;
    }

    public async Task<PagedResult<Drug>> QueryDrugs(string query, int page, int size)
    {
        // TODO: Refactor this to not load the entire DB on the server if performance's an issue
        var minimalSize = size > MaxItemsPerPage ? MaxItemsPerPage : size;
        var filtered = (await AppDbContext.Drugs.ToListAsync())
            .Select(d => new
            {
                Drug = d,
                Process.ExtractOne(query.ToUpperLatin(),
                        [
                            d.GenericName?.ToUpperLatin() ?? "",
                            d.LatinName?.ToUpperLatin() ?? "",
                            d.Atc?.ToUpperLatin() ?? "",
                            d.Ingredients?.ToUpperLatin() ?? ""
                        ],
                        s => s,
                        ScorerCache.Get<PartialRatioScorer>())
                    .Score
            })
            .Where(d => d.Score > 75)
            .OrderByDescending(d => d.Score)
            .Select(d => d.Drug)
            .ToList();

        var total = filtered.Count;

        var results = filtered
            .Skip(page * minimalSize)
            .Take(minimalSize);

        return new PagedResult<Drug>(results, total, page, minimalSize);
    }

    public async Task<PagedResult<Drug>> GetDrugsPaginated(int page, int size)
    {
        var minimalSize = size > MaxItemsPerPage ? MaxItemsPerPage : size;
        var total = await AppDbContext.Drugs.CountAsync();
        
        var drugs = await AppDbContext.Drugs
            .OrderBy(d => d.GenericName)
            .Skip(page * minimalSize)
            .Take(minimalSize)
            .ToListAsync();

        return new PagedResult<Drug>(drugs, total, page, minimalSize);
    }

    public async Task<IEnumerable<Drug>> GetDrugsByIds(IEnumerable<Guid> ids)
    {
        return await AppDbContext.Drugs.Where(d => ids.Contains(d.Id)).ToListAsync();
    }
}