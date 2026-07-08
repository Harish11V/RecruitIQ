using System;
using MediatR;
using RecruitIQ.Common;

namespace RecruitIQ.Application.Features.Jobs.PublishJob;

public record PublishJobCommand(Guid JobId, byte[] RowVersion) : IRequest<Result<Guid>>;
