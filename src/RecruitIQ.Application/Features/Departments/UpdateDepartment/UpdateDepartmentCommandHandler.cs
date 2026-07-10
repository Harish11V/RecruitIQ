using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using RecruitIQ.Application.Common.Interfaces;
using RecruitIQ.Common;
using RecruitIQ.Domain.Entities;

namespace RecruitIQ.Application.Features.Departments.UpdateDepartment;

public class UpdateDepartmentCommandHandler : IRequestHandler<UpdateDepartmentCommand, Result>
{
    private readonly IRecruitIQDbContext _context;
    private readonly ITenantService _tenantService;

    public UpdateDepartmentCommandHandler(IRecruitIQDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<Result> Handle(UpdateDepartmentCommand request, CancellationToken cancellationToken)
    {
        var companyId = _tenantService.CompanyId;

        var department = _context.Departments
            .FirstOrDefault(d => d.Id == request.Id && d.CompanyId == companyId);

        if (department == null)
        {
            return Result.Failure("DepartmentNotFound");
        }

        // Verify Name uniqueness for this company tenant (excluding self)
        var nameExists = _context.Departments
            .Any(d => d.Name.ToLower() == request.Name.ToLower() && 
                      d.Id != request.Id && 
                      d.CompanyId == companyId);

        if (nameExists)
        {
            return Result.Failure("DepartmentNameAlreadyExists");
        }

        department.Name = request.Name;
        department.Description = request.Description;

        _context.Update(department);
        _context.SetOriginalRowVersion(department, request.RowVersion);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex) when (ex.GetType().Name == "DbUpdateConcurrencyException")
        {
            return Result.Failure("A concurrency conflict occurred. The department has been modified by another user.");
        }

        return Result.Success();
    }
}
