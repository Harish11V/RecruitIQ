using System;
using System.IO;
using MediatR;
using RecruitIQ.Common;

namespace RecruitIQ.Application.Features.Candidates.UploadResume;

public record UploadResumeCommand(
    Guid CandidateId,
    string FileName,
    string MimeType,
    long FileSize,
    Stream FileStream) : IRequest<Result<Guid>>;
