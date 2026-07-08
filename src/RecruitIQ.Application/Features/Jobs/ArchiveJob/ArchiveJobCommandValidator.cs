using System;
using FluentValidation;

namespace RecruitIQ.Application.Features.Jobs.ArchiveJob;

public class ArchiveJobCommandValidator : AbstractValidator<ArchiveJobCommand>
{
    public ArchiveJobCommandValidator()
    {
        RuleFor(x => x.JobId)
            .NotEmpty().WithMessage("Job ID is required.");

        RuleFor(x => x.RowVersion)
            .NotEmpty().WithMessage("RowVersion is required.");
    }
}
