using System;
using MediatR;
using RecruitIQ.Common;

namespace RecruitIQ.Application.Features.Candidates.DeleteResume;

public record DeleteResumeCommand(Guid CandidateId, Guid ResumeId) : IRequest<Result<bool>>;
