using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RecruitIQ.Application.Common.Interfaces;
using RecruitIQ.Common;
using RecruitIQ.Contracts;
using RecruitIQ.Domain.Entities;
using RecruitIQ.Domain.Enums;

namespace RecruitIQ.Application.Features.Candidates.GetCandidateById;

public class GetCandidateByIdQueryHandler : IRequestHandler<GetCandidateByIdQuery, Result<CandidateDetailsResponse>>
{
    private readonly IRecruitIQDbContext _context;

    public GetCandidateByIdQueryHandler(IRecruitIQDbContext context)
    {
        _context = context;
    }

    public async Task<Result<CandidateDetailsResponse>> Handle(GetCandidateByIdQuery request, CancellationToken cancellationToken)
    {
        // 1. Fetch Candidate with eager loaded aggregates
        var candidate = await _context.QueryReadOnly<Candidate>()
            .Include(c => c.Experiences)
            .Include(c => c.Educations)
            .Include(c => c.Certifications)
            .Include(c => c.Resumes)
            .Include(c => c.CandidateSkills)
                .ThenInclude(cs => cs.Skill)
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (candidate == null)
        {
            return Result<CandidateDetailsResponse>.Failure($"Candidate with ID '{request.Id}' was not found.");
        }

        // 2. Fetch Activity counts
        var applicationsCount = await _context.QueryReadOnly<RecruitIQ.Domain.Entities.Application>()
            .CountAsync(a => a.CandidateId == request.Id, cancellationToken);

        var interviewsCount = await _context.QueryReadOnly<Interview>()
            .CountAsync(i => i.Application.CandidateId == request.Id, cancellationToken);

        // 3. Map Resumes List
        var resumes = candidate.Resumes
            .Where(r => !r.IsDeleted)
            .OrderByDescending(r => r.IsPrimary)
            .ThenByDescending(r => r.UploadedDate)
            .Select(r => new CandidateResumeSummary(
                r.Id,
                r.FileName,
                r.OriginalFileName,
                r.FileSize,
                r.MimeType,
                r.UploadedDate,
                r.CreatedBy,
                r.ParserVersion ?? "v1.0.0",
                r.IsPrimary,
                r.StoragePath,
                r.Version
            ))
            .ToList();

        // 4. Map DTO Collections
        var experiences = candidate.Experiences
            .OrderByDescending(e => e.StartDate)
            .Select(e => new CandidateExperienceSummary(
                e.CompanyName,
                e.Role,
                e.StartDate,
                e.EndDate,
                e.Description
            ))
            .ToList();

        var educations = candidate.Educations
            .OrderByDescending(e => e.EndYear ?? e.StartYear)
            .Select(e => new CandidateEducationSummary(
                e.SchoolOrCollege,
                e.Degree,
                e.GPA,
                e.StartYear,
                e.EndYear
            ))
            .ToList();

        // Mapping Certification Year: Extract from IssueDate
        var certifications = candidate.Certifications
            .OrderByDescending(c => c.IssueDate)
            .Select(c => new CandidateCertificationSummary(
                c.Name,
                c.IssuingAuthority,
                c.IssueDate.Year
            ))
            .ToList();

        var skills = candidate.CandidateSkills
            .Select(cs => cs.Skill.Name)
            .ToList();

        // 5. Construct details payload
        var response = new CandidateDetailsResponse(
            candidate.Id,
            candidate.CandidateNumber,
            candidate.FirstName,
            candidate.LastName,
            candidate.Email,
            candidate.PhoneNumber,
            candidate.LinkedInUrl,
            candidate.Title,
            candidate.Status,
            candidate.YearsOfExperience,
            candidate.CreatedAt,
            candidate.UpdatedAt,
            candidate.RowVersion,
            
            // Resumes
            resumes,
            
            // Skills
            skills,
            
            // Overview collections
            experiences,
            educations,
            certifications,
            
            // Activity Summary
            applicationsCount,
            interviewsCount
        );

        return Result<CandidateDetailsResponse>.Success(response);
    }
}
