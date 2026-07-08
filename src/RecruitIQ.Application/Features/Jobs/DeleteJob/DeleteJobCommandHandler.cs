using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using RecruitIQ.Application.Common.Interfaces;
using RecruitIQ.Common;
using RecruitIQ.Domain.Entities;
using RecruitIQ.Domain.Enums;

namespace RecruitIQ.Application.Features.Jobs.DeleteJob;

public class DeleteJobCommandHandler : IRequestHandler<DeleteJobCommand, Result<Guid>>
{
    private readonly IRecruitIQDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly ICurrentUserService _currentUserService;

    public DeleteJobCommandHandler(
        IRecruitIQDbContext context,
        ITenantService tenantService,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _tenantService = tenantService;
        _currentUserService = currentUserService;
    }

    public async Task<Result<Guid>> Handle(DeleteJobCommand request, CancellationToken cancellationToken)
    {
        var companyId = _tenantService.CompanyId;

        // Fetch job using tracked query
        var job = _context.Jobs.FirstOrDefault(j => j.Id == request.JobId && j.CompanyId == companyId);
        if (job == null)
        {
            return Result<Guid>.Failure(ErrorCodes.JobNotFound);
        }

        // Validate archived status domain rule
        if (job.Status != JobStatus.Archived)
        {
            return Result<Guid>.Failure(ErrorCodes.JobMustBeArchivedBeforeDelete);
        }

        // Configure change tracker RowVersion original value for concurrency checks
        _context.SetOriginalRowVersion(job, request.RowVersion);

        // Future-proof dependency validation
        var dependencyResult = await ValidateDependenciesAsync(job, cancellationToken);
        if (!dependencyResult.IsSuccess)
        {
            return Result<Guid>.Failure(dependencyResult.Error ?? "Dependency validation failed.");
        }

        // Soft delete the job via EF Core Change Tracker (SoftDeleteInterceptor completes the properties)
        _context.Remove(job);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex) when (ex.GetType().Name == "DbUpdateConcurrencyException")
        {
            return Result<Guid>.Failure(ErrorCodes.ConcurrencyConflict);
        }

        // Fetch department name for activity log
        var departmentName = _context.Departments
            .Where(d => d.Id == job.DepartmentId)
            .Select(d => d.Name)
            .FirstOrDefault() ?? "Unknown";

        // Log Activity
        var actionText = $"Job Deleted: {job.Title} ({job.JobCode}) - {departmentName}";
        if (actionText.Length > 100)
        {
            actionText = actionText[..97] + "...";
        }

        Guid? currentUserId = null;
        if (Guid.TryParse(_currentUserService.UserId, out var parsedUserId))
        {
            currentUserId = parsedUserId;
        }

        var activity = new Activity
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            UserId = currentUserId,
            EntityId = job.Id,
            EntityName = "Jobs",
            Action = actionText,
            Timestamp = DateTime.UtcNow
        };
        _context.Add(activity);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(job.Id);
    }

    private Task<Result> ValidateDependenciesAsync(Job job, CancellationToken cancellationToken)
    {
        // Future dependency validations (e.g. active Applications, Interviews, Offers) will go here
        return Task.FromResult(Result.Success());
    }
}
