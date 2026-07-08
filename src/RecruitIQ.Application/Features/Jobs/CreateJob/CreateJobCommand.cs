using System;
using System.Collections.Generic;
using MediatR;
using RecruitIQ.Common;
using RecruitIQ.Domain.Enums;

namespace RecruitIQ.Application.Features.Jobs.CreateJob;

public record CreateJobCommand(
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
    List<Guid> RequiredSkills) : IRequest<Result<Guid>>;
