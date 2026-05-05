using System.ComponentModel.DataAnnotations;

namespace DrugRegistry.API.Domain;

public class DrugEanData
{
    [Key] public string EanCode { get; set; } = default!;

    public string? DecisionNumber { get; set; }
    public string? LatinName { get; set; }
    public string? GenericName { get; set; }
    public string? PharmaceuticalForm { get; set; }
    public string? Strength { get; set; }
    public string? Packaging { get; set; }
}