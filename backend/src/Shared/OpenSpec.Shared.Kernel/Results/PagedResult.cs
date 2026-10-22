namespace OpenSpec.Shared.Kernel.Results;

public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);
