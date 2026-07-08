using System;
using MediatR;
using RecruitIQ.Common;
using RecruitIQ.Contracts;

namespace RecruitIQ.Application.Features.Jobs.GetJobById;

public record GetJobByIdQuery(Guid JobId) : IRequest<Result<JobDetailsResponse>>;
