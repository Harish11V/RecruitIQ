using System;
using MediatR;
using RecruitIQ.Common;

namespace RecruitIQ.Application.Features.Candidates.CreateCandidate;

public record CreateCandidateCommand(
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber,
    string? LinkedInUrl,
    string? Title,
    int? YearsOfExperience) : IRequest<Result<Guid>>;
