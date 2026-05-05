using DrugRegistry.API.Database;
using DrugRegistry.API.Domain;
using DrugRegistry.API.Services.Interfaces;
using DrugRegistry.API.Utils;
using FuzzySharp;
using FuzzySharp.SimilarityRatio;
using FuzzySharp.SimilarityRatio.Scorer.StrategySensitive;
using Microsoft.EntityFrameworkCore;

namespace DrugRegistry.API.Services;

public class PharmacyService(AppDbContext appDbContext) : BaseDbService(appDbContext), IPharmacyService
{
    private const int MaxItemsPerPage = 20;

    public async Task<List<Pharmacy>> GetAllPharmacies()
    {
        return await AppDbContext.Pharmacies.ToListAsync();
    }

    public async Task<Pharmacy?> GetPharmacyById(Guid id)
    {
        return await AppDbContext.Pharmacies.FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<Pharmacy?> GetPharmacyByIdNumber(string idNumber)
    {
        return await AppDbContext.Pharmacies.FirstOrDefaultAsync(p => p.IdNumber == idNumber);
    }

    public async Task<Pharmacy?> GetPharmacyByUrl(Uri uri)
    {
        return await AppDbContext.Pharmacies.FirstOrDefaultAsync(p => p.Url == uri);
    }

    public async Task<Pharmacy?> GetPharmacyByNameAndAddress(string name, string address)
    {
        return await AppDbContext.Pharmacies.FirstOrDefaultAsync(p => p.Name == name && p.Address == address);
    }


    public async Task<Guid> AddPharmacy(Pharmacy pharmacy)
    {
        var res = await AppDbContext.AddAsync(pharmacy);
        await AppDbContext.SaveChangesAsync();
        return res.Entity.Id;
    }

    public async Task<Guid> UpdatePharmacy(Pharmacy pharmacy, Guid id)
    {
        var existingPharmacy = await GetPharmacyById(id);
        if (existingPharmacy is null) throw new ArgumentException("Invalid pharmacy id");
        existingPharmacy.IdNumber = pharmacy.IdNumber;
        existingPharmacy.TaxNumber = pharmacy.TaxNumber;
        existingPharmacy.Code = pharmacy.Code;
        existingPharmacy.Name = pharmacy.Name;
        existingPharmacy.Address = pharmacy.Address;
        existingPharmacy.Municipality = pharmacy.Municipality;
        existingPharmacy.Place = pharmacy.Place;
        existingPharmacy.PhoneNumber = pharmacy.PhoneNumber;
        existingPharmacy.Decision = pharmacy.Decision;
        existingPharmacy.Email = pharmacy.Email;
        existingPharmacy.Location = pharmacy.Location;
        existingPharmacy.Pharmacists = pharmacy.Pharmacists;
        existingPharmacy.Technicians = pharmacy.Technicians;
        existingPharmacy.Comment = pharmacy.Comment;
        existingPharmacy.PharmacyType = pharmacy.PharmacyType;
        existingPharmacy.Active = pharmacy.Active;
        existingPharmacy.Central = pharmacy.Central;
        existingPharmacy.Url = pharmacy.Url;
        existingPharmacy.LastUpdate = pharmacy.LastUpdate;
        await AppDbContext.SaveChangesAsync();
        return existingPharmacy.Id;
    }

    public async Task<PagedResult<Pharmacy>> GetPharmaciesByDistance(Location location, int page, int size,
        string? municipality, string? place)
    {
        var minimalSize = size > MaxItemsPerPage ? MaxItemsPerPage : size;
        var pharmacies = await AppDbContext.Pharmacies
            .Include(p => p.Location)
            .Where(p => municipality == null || municipality == p.Municipality)
            .Where(p => place == null || place == p.Place)
            .ToListAsync();
        var results = pharmacies.OrderBy(p =>
                p.Location is not null
                    ? GeoUtils.GetDistanceBetweenLocations(p.Location, location)
                    : double.MaxValue)
            .Skip(page * minimalSize)
            .Take(minimalSize);

        var total = pharmacies.Count;

        return new PagedResult<Pharmacy>(results, total, page, minimalSize);
    }

    public async Task<PagedResult<Pharmacy>> GetPharmaciesByQuery(string query, int page, int size,
        string? municipality, string? place)
    {
        var minimalSize = size > MaxItemsPerPage ? MaxItemsPerPage : size;
        var pharmacies = await AppDbContext.Pharmacies
            .Include(p => p.Location)
            .Where(p => municipality == null || municipality == p.Municipality)
            .Where(p => place == null || place == p.Place)
            .ToListAsync();

        var results = pharmacies
            .Select(p => new
            {
                Pharmacy = p,
                Process.ExtractOne(query.ToUpperLatin(),
                        new[]
                        {
                            p.Name?.ToUpperLatin() ?? string.Empty,
                            p.Address?.ToUpperLatin() ?? string.Empty
                        },
                        s => s,
                        ScorerCache.Get<TokenSetScorer>())
                    .Score
            })
            .Where(d => d.Score > 75)
            .OrderByDescending(d => d.Score)
            .Select(d => d.Pharmacy)
            .ToList();

        var total = results.Count;

        results = results
            .Skip(page * minimalSize)
            .Take(minimalSize)
            .ToList();

        return new PagedResult<Pharmacy>(results, total, page, minimalSize);
    }

    public async Task<PagedResult<Pharmacy>> GetPharmaciesPaginated(int page, int size, string? municipality,
        string? place)
    {
        var minimalSize = size > MaxItemsPerPage ? MaxItemsPerPage : size;
        var baseQuery = AppDbContext.Pharmacies
            .Include(p => p.Location)
            .Where(p => municipality == null || municipality == p.Municipality)
            .Where(p => place == null || place == p.Place);

        var total = await baseQuery.CountAsync();
        var results = await baseQuery
            .OrderBy(p => p.Name)
            .Skip(page * minimalSize)
            .Take(minimalSize)
            .ToListAsync();

        return new PagedResult<Pharmacy>(results, total, page, minimalSize);
    }

    public async Task<IEnumerable<string>> GetMunicipalitiesOrderedByFrequency()
    {
        return (await AppDbContext.Pharmacies
            .GroupBy(p => p.Municipality)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .Where(m => m != null)
            .ToListAsync())!;
    }

    public async Task<IEnumerable<string>> GetPlacesOrderedByFrequencyForMunicipality(string municipality)
    {
        return (await AppDbContext.Pharmacies
            .Where(p => p.Municipality == municipality)
            .GroupBy(p => p.Place)
            .OrderByDescending(g => g.Count())
            .Select(g => g.Key)
            .Where(m => m != null)
            .ToListAsync())!;
    }

    public async Task<IEnumerable<Pharmacy>> GetPharmaciesByIds(IEnumerable<Guid> ids)
    {
        return await AppDbContext.Pharmacies
            .Where(p => ids.Contains(p.Id))
            .Include(p => p.Location)
            .ToListAsync();
    }
}