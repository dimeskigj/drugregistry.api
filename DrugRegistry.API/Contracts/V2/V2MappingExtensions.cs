using DrugRegistry.API.Domain;

namespace DrugRegistry.API.Contracts.V2;

public static class V2MappingExtensions
{
    public static PagedResponse<DrugResponse> ToResponse(this PagedResult<Drug> paged)
    {
        return new PagedResponse<DrugResponse>(
            paged.Data.Select(ToResponse),
            paged.TotalCount,
            paged.Page,
            paged.Size
        );
    }

    public static PagedResponse<PharmacyResponse> ToResponse(this PagedResult<Pharmacy> paged)
    {
        return new PagedResponse<PharmacyResponse>(
            paged.Data.Select(ToResponse),
            paged.TotalCount,
            paged.Page,
            paged.Size
        );
    }

    public static DrugResponse ToResponse(this Drug drug)
    {
        return new DrugResponse(
            drug.Id,
            drug.DecisionNumber,
            drug.Atc,
            drug.LatinName,
            drug.GenericName,
            drug.IssuingType.ToString(),
            drug.Ingredients,
            drug.Packaging,
            drug.Strength,
            drug.PharmaceuticalForm,
            drug.Url?.ToString(),
            drug.ManualUrl?.ToString(),
            drug.ReportUrl?.ToString(),
            drug.DecisionDate,
            drug.ValidityDate,
            drug.ApprovalCarrier,
            drug.Manufacturer,
            drug.PriceWithVat,
            drug.PriceWithoutVat,
            drug.LastUpdate
        );
    }

    public static PharmacyResponse ToResponse(this Pharmacy pharmacy)
    {
        return new PharmacyResponse(
            pharmacy.Id,
            pharmacy.IdNumber,
            pharmacy.TaxNumber,
            pharmacy.Code,
            pharmacy.Name,
            pharmacy.Address,
            pharmacy.Municipality,
            pharmacy.Place,
            pharmacy.PhoneNumber,
            pharmacy.Decision,
            pharmacy.Email,
            pharmacy.Pharmacists,
            pharmacy.Technicians,
            pharmacy.Comment,
            pharmacy.PharmacyType?.ToString(),
            pharmacy.Central,
            pharmacy.Active,
            pharmacy.Location is null
                ? null
                : new LocationResponse(pharmacy.Location.Longitude, pharmacy.Location.Latitude),
            pharmacy.Url?.ToString(),
            pharmacy.LastUpdate
        );
    }
}