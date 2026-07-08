using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using RecruitIQ.Application.Common.Interfaces;
using RecruitIQ.Common;
using RecruitIQ.Domain.Entities;
using RecruitIQ.Domain.Enums;

namespace RecruitIQ.Application.Features.Jobs.PublishJob;

public class PublishJobCommandHandler : IRequestHandler<PublishJobCommand, Result<Guid>>
{
    private readonly IRecruitIQDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly ICurrentUserService _currentUserService;

    public PublishJobCommandHandler(
        IRecruitIQDbContext context,
        ITenantService tenantService,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _tenantService = tenantService;
        _currentUserService = currentUserService;
    }

    public async Task<Result<Guid>> Handle(PublishJobCommand request, CancellationToken cancellationToken)
    {
        var companyId = _tenantService.CompanyId;

        // Fetch job using tracked query
        var job = _context.Jobs.FirstOrDefault(j => j.Id == request.JobId && j.CompanyId == companyId);
        if (job == null)
        {
            return Result<Guid>.Failure(ErrorCodes.JobNotFound);
        }

        // Validate status transitions
        if (job.Status == JobStatus.Published)
        {
            return Result<Guid>.Failure("Job is already published.");
        }
        if (job.Status == JobStatus.Closed)
        {
            return Result<Guid>.Failure("Closed jobs cannot be published.");
        }
        if (job.Status == JobStatus.Archived)
        {
            return Result<Guid>.Failure("Archived jobs cannot be published.");
        }
        if (job.Status != JobStatus.Draft)
        {
            return Result<Guid>.Failure("Only draft jobs can be published.");
        }

        // Set original RowVersion in EF change tracker for built-in optimistic concurrency check
        _context.SetOriginalRowVersion(job, request.RowVersion);

        // Validate department still exists in current tenant
        var department = _context.Departments.FirstOrDefault(d => d.Id == job.DepartmentId && d.CompanyId == companyId);
        if (department == null)
        {
            return Result<Guid>.Failure("Department does not exist or has been deleted.");
        }

        // Validate required fields
        if (string.IsNullOrWhiteSpace(job.Title))
        {
            return Result<Guid>.Failure("Title is required.");
        }
        if (string.IsNullOrWhiteSpace(job.Description))
        {
            return Result<Guid>.Failure("Description is required.");
        }
        if (string.IsNullOrWhiteSpace(job.Requirements))
        {
            return Result<Guid>.Failure("Requirements are required.");
        }

        // Validate skills count
        var hasSkills = _context.JobSkills.Any(js => js.JobId == job.Id);
        if (!hasSkills)
        {
            return Result<Guid>.Failure("A job must have at least one required skill to be published.");
        }

        // Validate closing date
        if (job.ClosingDate.HasValue && job.ClosingDate.Value.Date < DateTime.UtcNow.Date)
        {
            return Result<Guid>.Failure("Closing date cannot be in the past.");
        }

        // Update state
        job.Status = JobStatus.Published;
        if (job.PublishedAt == null)
        {
            job.PublishedAt = DateTime.UtcNow;
        }

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex) when (ex.GetType().Name == "DbUpdateConcurrencyException")
        {
            return Result<Guid>.Failure(ErrorCodes.ConcurrencyConflict);
        }

        // Log Activity
        var actionText = $"Job Published: {job.Title} ({job.JobCode}) - {department.Name}";
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
