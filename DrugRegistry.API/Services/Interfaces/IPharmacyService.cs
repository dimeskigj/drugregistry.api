using DrugRegistry.API.Domain;

namespace DrugRegistry.API.Services.Interfaces;

public interface IPharmacyService
{
    Task<List<Pharmacy>> GetAllPharmacies();
    Task<Pharmacy?> GetPharmacyById(Guid id);
    Task<Pharmacy?> GetPharmacyByIdNumber(string idNumber);
    Task<Pharmacy?> GetPharmacyByUrl(Uri uri);
    Task<Pharmacy?> GetPharmacyByNameAndAddress(string name, string address);
    Task<Guid> AddPharmacy(Pharmacy pharmacy);
    Task<Guid> UpdatePharmacy(Pharmacy pharmacy, Guid id);

    Task<PagedResult<Pharmacy>> GetPharmaciesByDistance(Location location, int page, int size, string? municipality,
        string? place);

    Task<PagedResult<Pharmacy>> GetPharmaciesByQuery(string query, int page, int size, string? municipality,
        string? place);

    Task<PagedResult<Pharmacy>> GetPharmaciesPaginated(int page, int size, string? municipality, string? place);

    Task<IEnumerable<string>> GetMunicipalitiesOrderedByFrequency();
    Task<IEnumerable<string>> GetPlacesOrderedByFrequencyForMunicipality(string municipality);
    Task<IEnumerable<Pharmacy>> GetPharmaciesByIds(IEnumerable<Guid> ids);
}