using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using RecruitIQ.Application.Common.Interfaces;
using RecruitIQ.Common;
using RecruitIQ.Domain.Entities;
using RecruitIQ.Domain.Enums;

namespace RecruitIQ.Application.Features.Jobs.CreateJob;

public class CreateJobCommandHandler : IRequestHandler<CreateJobCommand, Result<Guid>>
{
    private readonly IRecruitIQDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly ICurrentUserService _currentUserService;

    public CreateJobCommandHandler(
        IRecruitIQDbContext context,
        ITenantService tenantService,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _tenantService = tenantService;
        _currentUserService = currentUserService;
    }

    public async Task<Result<Guid>> Handle(CreateJobCommand request, CancellationToken cancellationToken)
    {
        var companyId = _tenantService.CompanyId;

        // 1. Validate Department belongs to the current tenant
        var department = _context.Departments
            .FirstOrDefault(d => d.Id == request.DepartmentId && d.CompanyId == companyId);

        if (department == null)
        {
            return Result<Guid>.Failure("Department does not exist or does not belong to the current tenant.");
        }

        // 2. Validate Hiring Manager belongs to the current tenant if provided
        if (request.HiringManagerId.HasValue)
        {
            var managerExists = _context.Users
                .Any(u => u.Id == request.HiringManagerId.Value && u.CompanyId == companyId);
            if (!managerExists)
            {
                return Result<Guid>.Failure("Hiring Manager does not exist or does not belong to the current tenant.");
            }
        }

        // 3. Load and Validate Skill entities
        var uniqueSkillIds = request.RequiredSkills.Distinct().ToList();
        var existingSkills = _context.Skills
            .Where(s => uniqueSkillIds.Contains(s.Id))
            .ToList();

        if (existingSkills.Count != uniqueSkillIds.Count)
        {
            return Result<Guid>.Failure("One or more required skills do not exist.");
        }

        // 4. Generate sequential JobCode: JOB-{Year}-{Sequence:D4}
        var jobCount = _context.Jobs.Count(j => j.CompanyId == companyId);
        var sequenceNumber = jobCount + 1;
        var jobCode = $"JOB-{DateTime.UtcNow.Year}-{sequenceNumber:D4}";

        // 5. Generate Slug: {normalized-title}-{Sequence:D4}
        var baseSlug = Regex.Replace(request.Title.ToLowerInvariant(), @"[^a-z0-9\s-]", "");
        baseSlug = Regex.Replace(baseSlug, @"\s+", "-").Trim('-');
        var slug = $"{baseSlug}-{sequenceNumber:D4}";

        // 6. Create Job entity (Draft status by default)
        var job = new Job
        {
            CompanyId = companyId,
            DepartmentId = request.DepartmentId,
            Title = request.Title,
            Description = request.Description,
            Requirements = request.Requirements,
            Location = request.Location,
            EmploymentType = request.EmploymentType,
            Status = JobStatus.Draft,
            SalaryMin = request.SalaryMin,
            SalaryMax = request.SalaryMax,
            HiringManagerId = request.HiringManagerId,
            ClosingDate = request.ClosingDate,
            JobCode = jobCode,
            Slug = slug,
            PublishedAt = null
        };

        _context.Add(job);

        // 7. Add Job Skills mapping loaded entities
        foreach (var skill in existingSkills)
        {
            var jobSkill = new JobSkill
            {
                CompanyId = companyId,
                JobId = job.Id,
                SkillId = skill.Id,
                IsRequired = true
            };
            _context.Add(jobSkill);
        }

        // 8. Log Activity with detailed metadata (title, department, job code)
        Guid? currentUserId = null;
        if (Guid.TryParse(_currentUserService.UserId, out var parsedUserId))
        {
            currentUserId = parsedUserId;
        }

        var actionText = $"Job Created: {job.Title} ({department.Name}) [{jobCode}]";
        if (actionText.Length > 100)
        {
            actionText = actionText[..97] + "...";
        }

        var activity = new Activity
        {
            CompanyId = companyId,
            UserId = currentUserId,
            Action = actionText,
            EntityName = "Jobs",
            EntityId = job.Id,
            Timestamp = DateTime.UtcNow
        };
        _context.Add(activity);

        await _context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(job.Id);
    }
}
