using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using RecruitIQ.Application.Common.Interfaces;
using RecruitIQ.Common;
using RecruitIQ.Contracts;

namespace RecruitIQ.Application.Features.Departments.GetDepartments;

public class GetDepartmentsQueryHandler : IRequestHandler<GetDepartmentsQuery, Result<IReadOnlyList<DepartmentSummaryResponse>>>
{
    private readonly IRecruitIQDbContext _context;

    public GetDepartmentsQueryHandler(IRecruitIQDbContext context)
    {
        _context = context;
    }

    public Task<Result<IReadOnlyList<DepartmentSummaryResponse>>> Handle(GetDepartmentsQuery request, CancellationToken cancellationToken)
    {
        var departments = _context.Departments
            .OrderBy(d => d.Name)
            .Select(d => new DepartmentSummaryResponse(d.Id, d.Name))
            .ToList();

        return Task.FromResult(Result<IReadOnlyList<DepartmentSummaryResponse>>.Success(departments));
    }
}
