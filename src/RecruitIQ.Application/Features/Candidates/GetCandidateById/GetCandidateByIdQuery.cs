using System;
using MediatR;
using RecruitIQ.Common;
using RecruitIQ.Contracts;

namespace RecruitIQ.Application.Features.Candidates.GetCandidateById;

public record GetCandidateByIdQuery(Guid Id) : IRequest<Result<CandidateDetailsResponse>>;
