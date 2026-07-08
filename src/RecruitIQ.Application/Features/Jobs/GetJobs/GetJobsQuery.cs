using System;
using MediatR;
using RecruitIQ.Common;
using RecruitIQ.Contracts;
using RecruitIQ.Domain.Enums;

namespace RecruitIQ.Application.Features.Jobs.GetJobs;

public record GetJobsQuery(
    int Page,
    int PageSize,
    string? Search,
    JobSortOption SortBy,
    JobStatus? Status,
    Guid? DepartmentId,
    EmploymentType? EmploymentType,
    Guid? HiringManagerId,
    string? Location) : IRequest<Result<PagedResponse<JobSummaryResponse>>>;
