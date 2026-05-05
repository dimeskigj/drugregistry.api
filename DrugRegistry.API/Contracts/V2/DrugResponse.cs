namespace DrugRegistry.API.Contracts.V2;

public sealed record DrugResponse(
    Guid Id,
    string? DecisionNumber,
    string? Atc,
    string? LatinName,
    string? GenericName,
    string IssuingType,
    string? Ingredients,
    string? Packaging,
    string? Strength,
    string? PharmaceuticalForm,
    string? Url,
    string? ManualUrl,
    string? ReportUrl,
    DateTime? DecisionDate,
    DateTime? ValidityDate,
    string? ApprovalCarrier,
    string? Manufacturer,
    double PriceWithVat,
    double PriceWithoutVat,
    DateTime LastUpdate
);