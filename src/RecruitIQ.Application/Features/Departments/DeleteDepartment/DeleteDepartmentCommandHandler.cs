using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using RecruitIQ.Application.Common.Interfaces;
using RecruitIQ.Common;
using RecruitIQ.Domain.Entities;

namespace RecruitIQ.Application.Features.Departments.DeleteDepartment;

public class DeleteDepartmentCommandHandler : IRequestHandler<DeleteDepartmentCommand, Result>
{
    private readonly IRecruitIQDbContext _context;
    private readonly ITenantService _tenantService;

    public DeleteDepartmentCommandHandler(IRecruitIQDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<Result> Handle(DeleteDepartmentCommand request, CancellationToken cancellationToken)
    {
        var companyId = _tenantService.CompanyId;

        var department = _context.Departments
            .FirstOrDefault(d => d.Id == request.Id && d.CompanyId == companyId);

        if (department == null)
        {
            return Result.Failure("DepartmentNotFound");
        }

        // Business Rule Guard: Prevent deletion of departments with active jobs
        var hasActiveJobs = _context.Jobs
            .Any(j => j.DepartmentId == request.Id && !j.IsDeleted);

        if (hasActiveJobs)
        {
            return Result.Failure("DepartmentHasActiveJobs");
        }

        _context.Remove(department);
        _context.SetOriginalRowVersion(department, request.RowVersion);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex) when (ex.GetType().Name == "DbUpdateConcurrencyException")
        {
            return Result.Failure("A concurrency conflict occurred. The department has been modified or deleted by another user.");
        }

        return Result.Success();
    }
}
