using System;
using FluentValidation;

namespace RecruitIQ.Application.Features.Candidates.ChangeCandidateStatus;

public class ChangeCandidateStatusCommandValidator : AbstractValidator<ChangeCandidateStatusCommand>
{
    public ChangeCandidateStatusCommandValidator()
    {
        RuleFor(x => x.CandidateId)
            .NotEmpty().WithMessage("Candidate ID is required.");

        RuleFor(x => x.NewStatus)
            .IsInEnum().WithMessage("Invalid Candidate Status value.");

        RuleFor(x => x.RowVersion)
            .NotEmpty().WithMessage("Row version is required for concurrency validation.");
    }
}
