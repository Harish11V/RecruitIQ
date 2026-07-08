using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using RecruitIQ.Application.Common.Interfaces;
using RecruitIQ.Common;
using RecruitIQ.Domain.Entities;
using RecruitIQ.Domain.Enums;

namespace RecruitIQ.Application.Features.Jobs.ArchiveJob;

public class ArchiveJobCommandHandler : IRequestHandler<ArchiveJobCommand, Result<Guid>>
{
    private readonly IRecruitIQDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly ICurrentUserService _currentUserService;

    public ArchiveJobCommandHandler(
        IRecruitIQDbContext context,
        ITenantService tenantService,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _tenantService = tenantService;
        _currentUserService = currentUserService;
    }

    public async Task<Result<Guid>> Handle(ArchiveJobCommand request, CancellationToken cancellationToken)
    {
        var companyId = _tenantService.CompanyId;

        // Fetch job using tracked query
        var job = _context.Jobs.FirstOrDefault(j => j.Id == request.JobId && j.CompanyId == companyId);
        if (job == null)
        {
            return Result<Guid>.Failure(ErrorCodes.JobNotFound);
        }

        // Validate status transitions
        if (job.Status == JobStatus.Archived)
        {
            return Result<Guid>.Failure("Job is already archived.");
        }
        if (job.Status == JobStatus.Draft)
        {
            return Result<Guid>.Failure("Draft jobs cannot be archived.");
        }
        if (job.Status == JobStatus.Closed)
        {
            return Result<Guid>.Failure("Closed jobs cannot be archived.");
        }
        if (job.Status != JobStatus.Published)
        {
            return Result<Guid>.Failure("Only published jobs can be archived.");
        }

        // Set original RowVersion in EF change tracker for built-in optimistic concurrency check
        _context.SetOriginalRowVersion(job, request.RowVersion);

        // Update state
        job.Status = JobStatus.Archived;

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex) when (ex.GetType().Name == "DbUpdateConcurrencyException")
        {
            return Result<Guid>.Failure(ErrorCodes.ConcurrencyConflict);
        }

        // Fetch department name for metadata log
        var departmentName = _context.Departments
            .Where(d => d.Id == job.DepartmentId)
            .Select(d => d.Name)
            .FirstOrDefault() ?? "Unknown";

        // Log Activity
        var actionText = $"Job Archived: {job.Title} ({job.JobCode}) - {departmentName}";
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
}
