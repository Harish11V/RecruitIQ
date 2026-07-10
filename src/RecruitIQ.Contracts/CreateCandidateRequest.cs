namespace RecruitIQ.Contracts;

public record CreateCandidateRequest(
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber,
    string? LinkedInUrl,
    string? Title,
    int? YearsOfExperience);
