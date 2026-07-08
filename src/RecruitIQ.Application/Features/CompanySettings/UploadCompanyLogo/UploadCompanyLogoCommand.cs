using System.IO;
using MediatR;
using RecruitIQ.Common;

namespace RecruitIQ.Application.Features.CompanySettings.UploadCompanyLogo;

public record UploadCompanyLogoCommand(
    Stream FileStream,
    string FileName,
    string ContentType) : IRequest<Result<string>>;
