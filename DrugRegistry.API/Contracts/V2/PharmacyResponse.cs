namespace DrugRegistry.API.Contracts.V2;

public sealed record PharmacyResponse(
    Guid Id,
    string? IdNumber,
    string? TaxNumber,
    string? Code,
    string? Name,
    string? Address,
    string? Municipality,
    string? Place,
    string? PhoneNumber,
    string? Decision,
    string? Email,
    string? Pharmacists,
    string? Technicians,
    string? Comment,
    string? PharmacyType,
    bool? Central,
    bool? Active,
    LocationResponse? Location,
    string? Url,
    DateTime LastUpdate
);