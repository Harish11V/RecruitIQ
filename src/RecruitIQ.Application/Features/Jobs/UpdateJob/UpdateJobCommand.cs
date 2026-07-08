using System;
using System.Collections.Generic;
using MediatR;
using RecruitIQ.Common;
using RecruitIQ.Domain.Enums;

namespace RecruitIQ.Application.Features.Jobs.UpdateJob;

public record UpdateJobCommand(
    Guid JobId,
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
    byte[] RowVersion) : IRequest<Result<Guid>>;
