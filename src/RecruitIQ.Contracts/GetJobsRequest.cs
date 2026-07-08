using System;
using RecruitIQ.Domain.Enums;

namespace RecruitIQ.Contracts;

public record GetJobsRequest(
    int Page = 1,
    int PageSize = 10,
    string? Search = null,
    JobSortOption SortBy = JobSortOption.CreatedAtDesc,
    JobStatus? Status = null,
    Guid? DepartmentId = null,
    EmploymentType? EmploymentType = null,
    Guid? HiringManagerId = null,
    string? Location = null);
