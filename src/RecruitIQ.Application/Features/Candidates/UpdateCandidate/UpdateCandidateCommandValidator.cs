using System;
using FluentValidation;

namespace RecruitIQ.Application.Features.Candidates.UpdateCandidate;

public class UpdateCandidateCommandValidator : AbstractValidator<UpdateCandidateCommand>
{
    public UpdateCandidateCommandValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(50).WithMessage("First name must not exceed 50 characters.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required.")
            .MaximumLength(50).WithMessage("Last name must not exceed 50 characters.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email address is required.")
            .EmailAddress().WithMessage("A valid email address is required.");

        RuleFor(x => x.LinkedInUrl)
            .Must(url => string.IsNullOrEmpty(url) || url.ToLower().Contains("linkedin.com"))
            .WithMessage("LinkedIn URL must be a valid LinkedIn link containing 'linkedin.com'.");

        RuleFor(x => x.YearsOfExperience)
            .InclusiveBetween(0, 50).When(x => x.YearsOfExperience.HasValue)
            .WithMessage("Years of experience must be between 0 and 50.");

        RuleFor(x => x.RowVersion)
            .NotEmpty().WithMessage("Row version is required for optimistic concurrency verification.");
    }
}
