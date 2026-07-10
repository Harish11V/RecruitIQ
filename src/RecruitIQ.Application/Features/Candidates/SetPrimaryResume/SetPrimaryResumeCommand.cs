using System;
using MediatR;
using RecruitIQ.Common;

namespace RecruitIQ.Application.Features.Candidates.SetPrimaryResume;

public record SetPrimaryResumeCommand(Guid CandidateId, Guid ResumeId) : IRequest<Result<bool>>;
