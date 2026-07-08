using MediatR;
using RecruitIQ.Common;

namespace RecruitIQ.Application.Features.CompanySettings.UpdateCompanySettings;

public record UpdateCompanySettingsCommand(
    string Theme,
    string Timezone,
    int DefaultInterviewDuration,
    string? AllowedEmailDomain) : IRequest<Result>;
