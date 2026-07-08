using System;
using System.Collections.Generic;
using RecruitIQ.Domain.Enums;

namespace RecruitIQ.Contracts;

public record CreateJobRequest(
    string Title,
    string Description,
    string Requirements,
    string Location,
    EmploymentType EmploymentType,
    decimal? SalaryMin,
    decimal? SalaryMax,
    Guid? HiringManagerId,
    Guid DepartmentId,
    DateTime? ClosingDate,
    List<Guid>? RequiredSkills);
