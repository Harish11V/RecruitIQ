using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using RecruitIQ.Application.Common.Interfaces;
using RecruitIQ.Common;
using RecruitIQ.Contracts;
using RecruitIQ.Domain.Entities;
using RecruitIQ.Domain.Enums;

namespace RecruitIQ.Application.Features.Jobs.GetJobs;

public class GetJobsQueryHandler : IRequestHandler<GetJobsQuery, Result<PagedResponse<JobSummaryResponse>>>
{
    private readonly IRecruitIQDbContext _context;
    private readonly ITenantService _tenantService;

    public GetJobsQueryHandler(IRecruitIQDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public Task<Result<PagedResponse<JobSummaryResponse>>> Handle(GetJobsQuery request, CancellationToken cancellationToken)
    {
        var companyId = _tenantService.CompanyId;

        // Start with tenant isolation filter using read-only query mapping
        var query = _context.QueryReadOnly<Job>().Where(j => j.CompanyId == companyId);

        // 1. Search (Title, JobCode, Description, DepartmentName) - Case-insensitive
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var searchLower = request.Search.ToLower();
            query = query.Where(j => j.Title.ToLower().Contains(searchLower) ||
                                     j.JobCode.ToLower().Contains(searchLower) ||
                                     j.Description.ToLower().Contains(searchLower) ||
                                     j.Department.Name.ToLower().Contains(searchLower));
        }

        // 2. Filters
        if (request.Status.HasValue)
        {
            query = query.Where(j => j.Status == request.Status.Value);
        }

        if (request.DepartmentId.HasValue)
        {
            query = query.Where(j => j.DepartmentId == request.DepartmentId.Value);
        }

        if (request.EmploymentType.HasValue)
        {
            query = query.Where(j => j.EmploymentType == request.EmploymentType.Value);
        }

        if (request.HiringManagerId.HasValue)
        {
            query = query.Where(j => j.HiringManagerId == request.HiringManagerId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Location))
        {
            var locationLower = request.Location.ToLower();
            query = query.Where(j => j.Location.ToLower() == locationLower);
        }

        // 3. Sorting
        query = request.SortBy switch
        {
            JobSortOption.CreatedAtAsc => query.OrderBy(j => j.CreatedAt),
            JobSortOption.TitleAsc => query.OrderBy(j => j.Title),
            JobSortOption.ClosingDateAsc => query.OrderBy(j => j.ClosingDate),
            _ => query.OrderByDescending(j => j.CreatedAt) // CreatedAtDesc default
        };

        // 4. Counts
        var totalRecords = query.Count();
        var totalPages = (int)Math.Ceiling((double)totalRecords / request.PageSize);

        // 5. Pagination and direct Projection to DTOs
        var items = query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(j => new JobSummaryResponse(
                j.Id,
                j.JobCode,
                j.Title,
                j.Department.Name,
                j.EmploymentType,
                j.Status,
                j.Location,
                j.CreatedAt,
                j.ClosingDate,
                0, // ApplicantCount defaults to 0
                j.RowVersion))
            .ToList();

        var response = new PagedResponse<JobSummaryResponse>(
            request.Page,
            request.PageSize,
            totalRecords,
            totalPages,
            items);

        return Task.FromResult(Result<PagedResponse<JobSummaryResponse>>.Success(response));
    }
}
