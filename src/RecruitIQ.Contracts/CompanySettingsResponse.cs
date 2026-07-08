using System;

namespace RecruitIQ.Contracts;

public record CompanySettingsResponse(
    Guid CompanyId,
    string Theme,
    string? LogoUrl,
    string Timezone,
    int DefaultInterviewDuration,
    string? AllowedEmailDomain);
