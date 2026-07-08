using System;
using FluentValidation;

namespace RecruitIQ.Application.Features.Jobs.UpdateJob;

public class UpdateJobCommandValidator : AbstractValidator<UpdateJobCommand>
{
    public UpdateJobCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Title is required.")
            .MaximumLength(200).WithMessage("Title must not exceed 200 characters.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.");

        RuleFor(x => x.Requirements)
            .NotEmpty().WithMessage("Requirements are required.");

        RuleFor(x => x.Location)
            .NotEmpty().WithMessage("Location is required.")
            .MaximumLength(100).WithMessage("Location must not exceed 100 characters.");

        RuleFor(x => x.SalaryMin)
            .GreaterThanOrEqualTo(0).WithMessage("Minimum salary must be greater than or equal to 0.")
            .LessThanOrEqualTo(10000000).WithMessage("Minimum salary must not exceed 10,000,000.")
            .When(x => x.SalaryMin.HasValue);

        RuleFor(x => x.SalaryMax)
            .GreaterThanOrEqualTo(0).WithMessage("Maximum salary must be greater than or equal to 0.")
            .LessThanOrEqualTo(10000000).WithMessage("Maximum salary must not exceed 10,000,000.")
            .GreaterThanOrEqualTo(x => x.SalaryMin ?? 0)
                .WithMessage("Maximum salary must be greater than or equal to minimum salary.")
                .When(x => x.SalaryMax.HasValue && x.SalaryMin.HasValue);

        RuleFor(x => x.ClosingDate)
            .Must(date => !date.HasValue || date.Value.Date >= DateTime.UtcNow.Date)
            .WithMessage("Closing date cannot be in the past.");

        RuleFor(x => x.RequiredSkills)
            .Must(skills => skills == null || skills.Distinct().Count() == skills.Count)
            .WithMessage("Required skills cannot contain duplicates.");

        RuleFor(x => x.RowVersion)
            .NotEmpty().WithMessage("RowVersion is required.");
    }
}
