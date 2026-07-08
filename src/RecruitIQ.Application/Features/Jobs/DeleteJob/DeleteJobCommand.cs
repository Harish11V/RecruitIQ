using System;
using MediatR;
using RecruitIQ.Common;

namespace RecruitIQ.Application.Features.Jobs.DeleteJob;

public record DeleteJobCommand(Guid JobId, byte[] RowVersion) : IRequest<Result<Guid>>;
