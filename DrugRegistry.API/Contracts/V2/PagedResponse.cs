namespace DrugRegistry.API.Contracts.V2;

public sealed record PagedResponse<T>(
    IEnumerable<T> Data,
    int TotalCount,
    int Page,
    int Size
);