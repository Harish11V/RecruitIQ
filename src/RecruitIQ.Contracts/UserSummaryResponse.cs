using System;

namespace RecruitIQ.Contracts;

public record UserSummaryResponse(
    Guid Id,
    string FullName,
    string Email);
