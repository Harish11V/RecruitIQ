using System;
using FluentValidation;

namespace RecruitIQ.Application.Features.Jobs.PublishJob;

public class PublishJobCommandValidator : AbstractValidator<PublishJobCommand>
{
    public PublishJobCommandValidator()
    {
        RuleFor(x => x.JobId)
            .NotEmpty().WithMessage("Job ID is required.");

        RuleFor(x => x.RowVersion)
            .NotEmpty().WithMessage("RowVersion is required.");
    }
}
