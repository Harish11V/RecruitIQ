using MediatR;
using RecruitIQ.Common;
using RecruitIQ.Contracts;

namespace RecruitIQ.Application.Features.Candidates.GetCandidates;

public record GetCandidatesQuery(
    string? Search = null,
    string? Status = null,
    int Page = 1,
    int PageSize = 10,
    string? SortBy = null,
    string? SortOrder = null) : IRequest<Result<PagedResponse<CandidateSummaryResponse>>>;
