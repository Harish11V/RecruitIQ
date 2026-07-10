using System;
using MediatR;
using RecruitIQ.Common;
using RecruitIQ.Domain.Enums;

namespace RecruitIQ.Application.Features.Candidates.ChangeCandidateStatus;

public record ChangeCandidateStatusCommand(
    Guid CandidateId,
    CandidateStatus NewStatus,
    byte[] RowVersion) : IRequest<Result<byte[]>>;
