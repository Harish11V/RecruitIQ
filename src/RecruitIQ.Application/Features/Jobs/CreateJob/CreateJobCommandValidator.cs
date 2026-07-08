using System;
using FluentValidation;

namespace RecruitIQ.Application.Features.Jobs.CreateJob;

public class CreateJobCommandValidator : AbstractValidator<CreateJobCommand>
{
    public CreateJobCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200).WithMessage("Title cannot exceed 200 characters.");

        RuleFor(x => x.Description).NotEmpty().WithMessage("Description is required.");
        RuleFor(x => x.Requirements).NotEmpty().WithMessage("Requirements are required.");
        RuleFor(x => x.Location).NotEmpty().WithMessage("Location is required.");

        RuleFor(x => x.SalaryMin)
            .GreaterThanOrEqualTo(0).When(x => x.SalaryMin.HasValue)
            .LessThanOrEqualTo(10000000).When(x => x.SalaryMin.HasValue)
            .WithMessage("SalaryMin must be between 0 and 10,000,000.");

        RuleFor(x => x.SalaryMax)
            .GreaterThanOrEqualTo(x => x.SalaryMin ?? 0).When(x => x.SalaryMax.HasValue && x.SalaryMin.HasValue)
            .LessThanOrEqualTo(10000000).When(x => x.SalaryMax.HasValue)
            .WithMessage("SalaryMax must be greater than or equal to SalaryMin and less than or equal to 10,000,000.");

        RuleFor(x => x.ClosingDate)
            .Must(date => !date.HasValue || date.Value.Date >= DateTime.UtcNow.Date)
            .WithMessage("ClosingDate cannot be in the past.");

        RuleFor(x => x.RequiredSkills)
            .Must(skills => skills == null || skills.Count == 0 || skills.Count == new System.Collections.Generic.HashSet<Guid>(skills).Count)
            .WithMessage("RequiredSkills cannot contain duplicates.");
    }
}
