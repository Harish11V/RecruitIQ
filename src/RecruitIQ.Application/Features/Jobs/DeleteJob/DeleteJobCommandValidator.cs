using System;
using FluentValidation;

namespace RecruitIQ.Application.Features.Jobs.DeleteJob;

public class DeleteJobCommandValidator : AbstractValidator<DeleteJobCommand>
{
    public DeleteJobCommandValidator()
    {
        RuleFor(x => x.JobId)
            .NotEmpty().WithMessage("Job ID is required.");

        RuleFor(x => x.RowVersion)
            .NotEmpty().WithMessage("RowVersion is required.");
    }
}
