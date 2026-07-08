using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using RecruitIQ.Application.Common.Interfaces;
using RecruitIQ.Common;
using RecruitIQ.Domain.Entities;
using RecruitIQ.Domain.Enums;

namespace RecruitIQ.Application.Features.Jobs.UpdateJob;

public class UpdateJobCommandHandler : IRequestHandler<UpdateJobCommand, Result<Guid>>
{
    private readonly IRecruitIQDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly ICurrentUserService _currentUserService;

    public UpdateJobCommandHandler(
        IRecruitIQDbContext context,
        ITenantService tenantService,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _tenantService = tenantService;
        _currentUserService = currentUserService;
    }

    public async Task<Result<Guid>> Handle(UpdateJobCommand request, CancellationToken cancellationToken)
    {
        var companyId = _tenantService.CompanyId;

        // Fetch job using tracked query
        var job = _context.Jobs.FirstOrDefault(j => j.Id == request.JobId && j.CompanyId == companyId);
        if (job == null)
        {
            return Result<Guid>.Failure(ErrorCodes.JobNotFound);
        }

        // Validate archived state
        if (job.Status == JobStatus.Archived)
        {
            return Result<Guid>.Failure("Archived jobs cannot be edited.");
        }

        // Set original RowVersion in EF change tracker for built-in optimistic concurrency check
        _context.SetOriginalRowVersion(job, request.RowVersion);

        // Validate Department
        var departmentExists = _context.Departments.Any(d => d.Id == request.DepartmentId && d.CompanyId == companyId);
        if (!departmentExists)
        {
            return Result<Guid>.Failure("Department does not exist or does not belong to the current tenant.");
        }

        // Validate Hiring Manager
        if (request.HiringManagerId.HasValue)
        {
            var hmExists = _context.Users.Any(u => u.Id == request.HiringManagerId.Value && u.CompanyId == companyId);
            if (!hmExists)
            {
                return Result<Guid>.Failure("Hiring manager does not exist or does not belong to the current tenant.");
            }
        }

        // Validate Skills
        var skillIds = request.RequiredSkills ?? new List<Guid>();
        if (skillIds.Any())
        {
            var skillsCount = _context.Skills.Count(s => skillIds.Contains(s.Id));
            if (skillsCount != skillIds.Count)
            {
                return Result<Guid>.Failure("One or more required skills do not exist.");
            }
        }

        // Track changed fields
        var changedFields = new List<string>();
        if (job.Title != request.Title) { changedFields.Add("Title"); job.Title = request.Title; }
        if (job.Description != request.Description) { changedFields.Add("Description"); job.Description = request.Description; }
        if (job.Requirements != request.Requirements) { changedFields.Add("Requirements"); job.Requirements = request.Requirements; }
        if (job.DepartmentId != request.DepartmentId) { changedFields.Add("Department"); job.DepartmentId = request.DepartmentId; }
        if (job.HiringManagerId != request.HiringManagerId) { changedFields.Add("HiringManager"); job.HiringManagerId = request.HiringManagerId; }
        if (job.EmploymentType != request.EmploymentType) { changedFields.Add("EmploymentType"); job.EmploymentType = request.EmploymentType; }
        if (job.SalaryMin != request.SalaryMin) { changedFields.Add("SalaryMin"); job.SalaryMin = request.SalaryMin; }
        if (job.SalaryMax != request.SalaryMax) { changedFields.Add("SalaryMax"); job.SalaryMax = request.SalaryMax; }
        if (job.Location != request.Location) { changedFields.Add("Location"); job.Location = request.Location; }
        if (job.ClosingDate != request.ClosingDate) { changedFields.Add("ClosingDate"); job.ClosingDate = request.ClosingDate; }

        // Sync Skills
        var existingSkills = _context.JobSkills.Where(js => js.JobId == job.Id).ToList();
        var existingSkillIds = existingSkills.Select(js => js.SkillId).ToList();

        // Remove skills not in the request
        foreach (var existingSkill in existingSkills)
        {
            if (!skillIds.Contains(existingSkill.SkillId))
            {
                _context.Remove(existingSkill);
                changedFields.Add("RequiredSkills");
            }
        }

        // Add new skills
        foreach (var newSkillId in skillIds)
        {
            if (!existingSkillIds.Contains(newSkillId))
            {
                var jobSkill = new JobSkill
                {
                    Id = Guid.NewGuid(),
                    CompanyId = companyId,
                    JobId = job.Id,
                    SkillId = newSkillId
                };
                _context.Add(jobSkill);
                changedFields.Add("RequiredSkills");
            }
        }

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex) when (ex.GetType().Name == "DbUpdateConcurrencyException")
        {
            return Result<Guid>.Failure(ErrorCodes.ConcurrencyConflict);
        }

        // Log Activity only if actual updates occurred
        if (changedFields.Any())
        {
            var changedFieldsStr = string.Join(", ", changedFields.Distinct());
            var actionText = $"Job Updated: {job.Title} ({job.JobCode}). Changed fields: {changedFieldsStr}";
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
        }

        return Result<Guid>.Success(job.Id);
    }
}
