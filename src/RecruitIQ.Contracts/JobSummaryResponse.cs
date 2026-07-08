using System;
using RecruitIQ.Domain.Enums;

namespace RecruitIQ.Contracts;

public record JobSummaryResponse(
    Guid Id,
    string JobCode,
    string Title,
    string DepartmentName,
    EmploymentType EmploymentType,
    JobStatus Status,
    string Location,
    DateTime CreatedAt,
    DateTime? ClosingDate,
    int ApplicantCount,
    byte[] RowVersion);
