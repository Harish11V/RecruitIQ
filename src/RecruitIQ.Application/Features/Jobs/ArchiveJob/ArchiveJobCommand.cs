using System;
using MediatR;
using RecruitIQ.Common;

namespace RecruitIQ.Application.Features.Jobs.ArchiveJob;

public record ArchiveJobCommand(Guid JobId, byte[] RowVersion) : IRequest<Result<Guid>>;
