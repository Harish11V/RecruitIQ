using System;
using System.Collections.Generic;
using RecruitIQ.Domain.Enums;

namespace RecruitIQ.Contracts;

public record UpdateJobRequest(
    string Title,
    string Description,
    string Requirements,
    string Responsibilities,
    Guid DepartmentId,
    Guid? HiringManagerId,
    EmploymentType EmploymentType,
    decimal? SalaryMin,
    decimal? SalaryMax,
    string Location,
    DateTime? ClosingDate,
    List<Guid> RequiredSkills,
    byte[] RowVersion);
