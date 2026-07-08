using System;
using System.Collections.Generic;
using RecruitIQ.Domain.Enums;

namespace RecruitIQ.Contracts;

public record JobDetailsResponse(
    Guid JobId,
    string JobCode,
    string Title,
    string Slug,
    string Description,
    string Requirements,
    string Responsibilities,
    DepartmentSummaryResponse Department,
    UserSummaryResponse? HiringManager,
    EmploymentType EmploymentType,
    JobStatus Status,
    string Location,
    decimal? SalaryMin,
    decimal? SalaryMax,
    DateTime? PublishedAt,
    DateTime? ClosingDate,
    DateTime CreatedAt,
    List<SkillSummaryResponse> RequiredSkills,
    int ApplicantCount,
    int InterviewCount,
    byte[] RowVersion);
