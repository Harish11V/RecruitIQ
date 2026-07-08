using MediatR;
using RecruitIQ.Common;
using RecruitIQ.Contracts;

namespace RecruitIQ.Application.Features.CompanySettings.GetCompanySettings;

public record GetCompanySettingsQuery() : IRequest<Result<CompanySettingsResponse>>;
