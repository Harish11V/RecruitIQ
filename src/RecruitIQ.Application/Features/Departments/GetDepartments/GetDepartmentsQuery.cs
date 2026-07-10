using System.Collections.Generic;
using MediatR;
using RecruitIQ.Common;
using RecruitIQ.Contracts;

namespace RecruitIQ.Application.Features.Departments.GetDepartments;

public record GetDepartmentsQuery(string? Search = null) : IRequest<Result<IReadOnlyList<DepartmentResponse>>>;
