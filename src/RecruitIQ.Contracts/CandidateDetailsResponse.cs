using System;
using System.Collections.Generic;
using RecruitIQ.Domain.Enums;

namespace RecruitIQ.Contracts;

public record CandidateExperienceSummary(
    string Company,
    string Role,
    DateTime StartDate,
    DateTime? EndDate,
    string? Description);

public record CandidateEducationSummary(
    string Institution,
    string Degree,
    decimal? GPA,
    int StartYear,
    int? EndYear);

public record CandidateCertificationSummary(
    string Certificate,
    string Provider,
    int Year);

public record CandidateResumeSummary(
    Guid Id,
    string FileName,
    string OriginalFileName,
    long FileSize,
    string MimeType,
    DateTime UploadedDate,
    string UploadedBy,
    string ParserVersion,
    bool IsPrimary,
    string StoragePath,
    int Version);

public record CandidateDetailsResponse(
    Guid CandidateId,
    string CandidateNumber,
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber,
    string? LinkedInUrl,
    string? Title,
    CandidateStatus Status,
    int? YearsOfExperience,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    byte[] RowVersion,
    
    // Resumes List
    IReadOnlyList<CandidateResumeSummary> Resumes,
    
    // Skills
    IReadOnlyList<string> Skills,
    
    // Overview Collections
    IReadOnlyList<CandidateExperienceSummary> Experiences,
    IReadOnlyList<CandidateEducationSummary> Educations,
    IReadOnlyList<CandidateCertificationSummary> Certifications,
    
    // Activity Summary
    int ApplicationsCount,
    int InterviewsCount);
