using System;
using MediatR;
using RecruitIQ.Common;
using RecruitIQ.Contracts;

namespace RecruitIQ.Application.Features.Departments.CreateDepartment;

public record CreateDepartmentCommand(
    string Name,
    string? Description) : IRequest<Result<Guid>>;
