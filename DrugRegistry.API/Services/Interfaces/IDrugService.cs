using DrugRegistry.API.Domain;

namespace DrugRegistry.API.Services.Interfaces;

public interface IDrugService
{
    Task<List<Drug>> GetAllDrugs();
    Task<Drug?> GetDrugById(Guid id);
    Task<Drug?> GetDrugByDecisionNumberAndAtc(string decisionNumber, string atc);
    Task<Drug?> GetDrugByUrl(Uri uri);
    Task<Guid> AddDrug(Drug drug);
    Task<Guid> UpdateDrug(Drug drug, Guid id);
    Task<PagedResult<Drug>> QueryDrugs(string query, int page, int size);
    Task<PagedResult<Drug>> GetDrugsPaginated(int page, int size);
    Task<IEnumerable<Drug>> GetDrugsByIds(IEnumerable<Guid> ids);
}