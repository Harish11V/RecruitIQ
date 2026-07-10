using System;
using MediatR;
using RecruitIQ.Common;

namespace RecruitIQ.Application.Features.Departments.UpdateDepartment;

public record UpdateDepartmentCommand(
    Guid Id,
    string Name,
    string? Description,
    byte[] RowVersion) : IRequest<Result>;
