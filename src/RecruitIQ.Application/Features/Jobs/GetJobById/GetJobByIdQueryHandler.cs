using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using RecruitIQ.Application.Common.Interfaces;
using RecruitIQ.Common;
using RecruitIQ.Contracts;
using RecruitIQ.Domain.Entities;

namespace RecruitIQ.Application.Features.Jobs.GetJobById;

public class GetJobByIdQueryHandler : IRequestHandler<GetJobByIdQuery, Result<JobDetailsResponse>>
{
    private readonly IRecruitIQDbContext _context;
    private readonly ITenantService _tenantService;

    public GetJobByIdQueryHandler(IRecruitIQDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public Task<Result<JobDetailsResponse>> Handle(GetJobByIdQuery request, CancellationToken cancellationToken)
    {
        var companyId = _tenantService.CompanyId;

        // Perform read-only query and project directly to DTO to avoid lazy loading/N+1 queries
        var jobDetails = _context.QueryReadOnly<Job>()
            .Where(j => j.Id == request.JobId && j.CompanyId == companyId)
            .Select(j => new JobDetailsResponse(
                j.Id,
                j.JobCode,
                j.Title,
                j.Slug,
                j.Description,
                j.Requirements,
                string.Empty, // Responsibilities (defaults to empty)
                new DepartmentSummaryResponse(j.Department.Id, j.Department.Name),
                j.HiringManager != null 
                    ? new UserSummaryResponse(j.HiringManager.Id, j.HiringManager.FirstName + " " + j.HiringManager.LastName, j.HiringManager.Email) 
                    : null,
                j.EmploymentType,
                j.Status,
                j.Location,
                j.SalaryMin,
                j.SalaryMax,
                j.PublishedAt,
                j.ClosingDate,
                j.CreatedAt,
                j.JobSkills.Select(js => new SkillSummaryResponse(js.Skill.Id, js.Skill.Name)).ToList(),
                0, // ApplicantCount (default 0)
                0, // InterviewCount (default 0)
                j.RowVersion
            ))
            .FirstOrDefault();

        if (jobDetails == null)
        {
            return Task.FromResult(Result<JobDetailsResponse>.Failure("Job not found."));
        }

        return Task.FromResult(Result<JobDetailsResponse>.Success(jobDetails));
    }
}
