using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using RecruitIQ.Application.Common.Interfaces;
using RecruitIQ.Common;
using RecruitIQ.Domain.Entities;

namespace RecruitIQ.Application.Features.Departments.CreateDepartment;

public class CreateDepartmentCommandHandler : IRequestHandler<CreateDepartmentCommand, Result<Guid>>
{
    private readonly IRecruitIQDbContext _context;
    private readonly ITenantService _tenantService;

    public CreateDepartmentCommandHandler(IRecruitIQDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<Result<Guid>> Handle(CreateDepartmentCommand request, CancellationToken cancellationToken)
    {
        var companyId = _tenantService.CompanyId;

        // Verify Name uniqueness for this company tenant
        var nameExists = _context.Departments
            .Any(d => d.Name.ToLower() == request.Name.ToLower() && d.CompanyId == companyId);

        if (nameExists)
        {
            return Result<Guid>.Failure("DepartmentNameAlreadyExists");
        }

        var department = new Department
        {
            CompanyId = companyId,
            Name = request.Name,
            Description = request.Description
        };

        _context.Add(department);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(department.Id);
    }
}
