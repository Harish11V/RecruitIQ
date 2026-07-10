using System;
using RecruitIQ.Domain.Enums;

namespace RecruitIQ.Contracts;

public record ChangeCandidateStatusRequest(
    CandidateStatus Status,
    byte[] RowVersion);
