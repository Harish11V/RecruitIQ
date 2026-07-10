using System;
using System.Collections.Generic;

namespace RecruitIQ.Contracts;

public record PersonSummary(
    string FirstName,
    string LastName,
    string? Title);

public record ContactSummary(
    string Email,
    string? PhoneNumber,
    string? LinkedInUrl);

public record StatusSummary(
    string Code,
    string Label,
    string Color);

public record ResumeSummary(
    bool HasResume,
    string? FileName,
    DateTime? UploadedAt);

public record CandidateSummaryResponse(
    Guid Id,
    string CandidateNumber,
    PersonSummary Person,
    ContactSummary Contact,
    StatusSummary Status,
    ResumeSummary Resume,
    List<string> Skills,
    int? YearsOfExperience);
