using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using RecruitIQ.Application.Common.Interfaces;
using RecruitIQ.Common;
using RecruitIQ.Contracts;
using RecruitIQ.Domain.Entities;
using RecruitIQ.Domain.Enums;

namespace RecruitIQ.Application.Features.Candidates.GetCandidates;

public class GetCandidatesQueryHandler : IRequestHandler<GetCandidatesQuery, Result<PagedResponse<CandidateSummaryResponse>>>
{
    private readonly IRecruitIQDbContext _context;
    private readonly ITenantService _tenantService;

    public GetCandidatesQueryHandler(IRecruitIQDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public Task<Result<PagedResponse<CandidateSummaryResponse>>> Handle(GetCandidatesQuery request, CancellationToken cancellationToken)
    {
        var companyId = _tenantService.CompanyId;

        // Query read-only from database context
        var query = _context.QueryReadOnly<Candidate>()
            .Where(c => c.CompanyId == companyId);

        // 1. Search (FirstName, LastName, Email, Title) - Case-insensitive
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var searchLower = request.Search.ToLower();
            query = query.Where(c => c.FirstName.ToLower().Contains(searchLower) ||
                                     c.LastName.ToLower().Contains(searchLower) ||
                                     c.Email.ToLower().Contains(searchLower) ||
                                     (c.Title != null && c.Title.ToLower().Contains(searchLower)));
        }

        // 2. Status Filter
        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            if (Enum.TryParse<CandidateStatus>(request.Status, true, out var statusEnum))
            {
                query = query.Where(c => c.Status == statusEnum);
            }
        }

        // 3. Sorting
        query = request.SortBy?.ToLower() switch
        {
            "firstname" => request.SortOrder?.ToLower() == "desc" ? query.OrderByDescending(c => c.FirstName) : query.OrderBy(c => c.FirstName),
            "lastname" => request.SortOrder?.ToLower() == "desc" ? query.OrderByDescending(c => c.LastName) : query.OrderBy(c => c.LastName),
            "title" => request.SortOrder?.ToLower() == "desc" ? query.OrderByDescending(c => c.Title) : query.OrderBy(c => c.Title),
            "status" => request.SortOrder?.ToLower() == "desc" ? query.OrderByDescending(c => c.Status) : query.OrderBy(c => c.Status),
            _ => query.OrderByDescending(c => c.CreatedAt) // CreatedAtDesc default
        };

        // 4. Counts
        var totalRecords = query.Count();
        var totalPages = (int)Math.Ceiling((double)totalRecords / request.PageSize);

        // 5. Direct projection to summary response records
        var items = query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(c => new CandidateSummaryResponse(
                c.Id,
                c.CandidateNumber,
                new PersonSummary(c.FirstName, c.LastName, c.Title),
                new ContactSummary(c.Email, c.PhoneNumber, c.LinkedInUrl),
                new StatusSummary(
                    c.Status.ToString(),
                    c.Status.ToString(),
                    c.Status == CandidateStatus.Available || c.Status == CandidateStatus.Hired ? "success" :
                    c.Status == CandidateStatus.Interviewing ? "warning" :
                    c.Status == CandidateStatus.Shortlisted ? "primary" :
                    c.Status == CandidateStatus.Offered ? "accent" :
                    c.Status == CandidateStatus.Rejected ? "warn" : "info"
                ),
                new ResumeSummary(
                    c.Resumes.Any(),
                    c.Resumes.OrderByDescending(r => r.IsPrimary).ThenByDescending(r => r.UploadedDate).Select(r => r.FileName).FirstOrDefault(),
                    c.Resumes.OrderByDescending(r => r.IsPrimary).ThenByDescending(r => r.UploadedDate).Select(r => (DateTime?)r.UploadedDate).FirstOrDefault()
                ),
                c.CandidateSkills.Select(cs => cs.Skill.Name).ToList(),
                c.YearsOfExperience
            ))
            .ToList();

        var response = new PagedResponse<CandidateSummaryResponse>(
            request.Page,
            request.PageSize,
            totalRecords,
            totalPages,
            items);

        return Task.FromResult(Result<PagedResponse<CandidateSummaryResponse>>.Success(response));
    }
}
