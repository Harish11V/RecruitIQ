using System;
using System.Collections.Generic;
using RecruitIQ.Domain.Enums;

namespace RecruitIQ.Contracts;

public record JobResponse(
    Guid Id,
    string JobCode,
    string Slug,
    Guid CompanyId,
    Guid DepartmentId,
    string Title,
    string Description,
    string Requirements,
    string Location,
    EmploymentType EmploymentType,
    JobStatus Status,
    decimal? SalaryMin,
    decimal? SalaryMax,
    Guid? HiringManagerId,
    DateTime? PublishedAt,
    DateTime? ClosingDate,
    List<Guid> RequiredSkills);
