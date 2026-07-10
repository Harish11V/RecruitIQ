using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using RecruitIQ.Application.Common.Interfaces;
using RecruitIQ.Common;
using RecruitIQ.Contracts;

namespace RecruitIQ.Application.Features.Departments.GetDepartments;

public class GetDepartmentsQueryHandler : IRequestHandler<GetDepartmentsQuery, Result<IReadOnlyList<DepartmentResponse>>>
{
    private readonly IRecruitIQDbContext _context;

    public GetDepartmentsQueryHandler(IRecruitIQDbContext context)
    {
        _context = context;
    }

    public Task<Result<IReadOnlyList<DepartmentResponse>>> Handle(GetDepartmentsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Departments.AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var searchLower = request.Search.ToLower();
            query = query.Where(d => d.Name.ToLower().Contains(searchLower) || 
                                     (d.Description != null && d.Description.ToLower().Contains(searchLower)));
        }

        var departments = query
            .OrderBy(d => d.Name)
            .Select(d => new DepartmentResponse(
                d.Id, 
                d.Name, 
                d.Description, 
                d.CreatedAt, 
                d.UpdatedAt, 
                d.RowVersion))
            .ToList();

        return Task.FromResult(Result<IReadOnlyList<DepartmentResponse>>.Success(departments));
    }
}
