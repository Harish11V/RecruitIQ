namespace RecruitIQ.Contracts;

public record UpdateCompanySettingsRequest(
    string Theme,
    string Timezone,
    int DefaultInterviewDuration,
    string? AllowedEmailDomain);
