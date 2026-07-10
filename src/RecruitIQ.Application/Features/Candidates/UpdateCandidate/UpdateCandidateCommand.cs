using System;
using MediatR;
using RecruitIQ.Common;
using RecruitIQ.Domain.Enums;

namespace RecruitIQ.Application.Features.Candidates.UpdateCandidate;

public record UpdateCandidateCommand(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber,
    string? LinkedInUrl,
    string? Title,
    CandidateStatus Status,
    int? YearsOfExperience,
    byte[] RowVersion) : IRequest<Result<byte[]>>;
