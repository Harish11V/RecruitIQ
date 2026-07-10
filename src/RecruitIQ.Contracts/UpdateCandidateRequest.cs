using System;
using RecruitIQ.Domain.Enums;

namespace RecruitIQ.Contracts;

public record UpdateCandidateRequest(
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber,
    string? LinkedInUrl,
    string? Title,
    CandidateStatus Status,
    int? YearsOfExperience,
    byte[] RowVersion);
