using System;
using System.Collections.Generic;
using MediatR;
using RecruitIQ.Common;
using RecruitIQ.Contracts;

namespace RecruitIQ.Application.Features.Candidates.GetCandidateTimeline;

public record GetCandidateTimelineQuery(Guid CandidateId) : IRequest<Result<IReadOnlyList<CandidateTimelineItemResponse>>>;
