using System;
using MediatR;
using RecruitIQ.Common;

namespace RecruitIQ.Application.Features.Departments.DeleteDepartment;

public record DeleteDepartmentCommand(
    Guid Id,
    byte[] RowVersion) : IRequest<Result>;
