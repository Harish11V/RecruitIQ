using System.Collections.Generic;

namespace RecruitIQ.Contracts;

public record PagedResponse<T>(
    int Page,
    int PageSize,
    int TotalRecords,
    int TotalPages,
    IReadOnlyList<T> Items)
{
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;
}
