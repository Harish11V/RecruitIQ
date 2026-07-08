using FluentValidation;

namespace RecruitIQ.Application.Features.CompanySettings.UpdateCompanySettings;

public class UpdateCompanySettingsCommandValidator : AbstractValidator<UpdateCompanySettingsCommand>
{
    public UpdateCompanySettingsCommandValidator()
    {
        RuleFor(x => x.Theme).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Timezone).NotEmpty().MaximumLength(50);
        RuleFor(x => x.DefaultInterviewDuration).GreaterThan(0);
        RuleFor(x => x.AllowedEmailDomain).MaximumLength(100);
    }
}
