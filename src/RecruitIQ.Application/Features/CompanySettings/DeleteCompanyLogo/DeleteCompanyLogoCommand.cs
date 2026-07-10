using MediatR;
using RecruitIQ.Common;

namespace RecruitIQ.Application.Features.CompanySettings.DeleteCompanyLogo;

public record DeleteCompanyLogoCommand : IRequest<Result>;
