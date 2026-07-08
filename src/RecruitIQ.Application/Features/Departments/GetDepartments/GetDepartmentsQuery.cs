using System.Collections.Generic;
using MediatR;
using RecruitIQ.Common;
using RecruitIQ.Contracts;

namespace RecruitIQ.Application.Features.Departments.GetDepartments;

public record GetDepartmentsQuery() : IRequest<Result<IReadOnlyList<DepartmentSummaryResponse>>>;
