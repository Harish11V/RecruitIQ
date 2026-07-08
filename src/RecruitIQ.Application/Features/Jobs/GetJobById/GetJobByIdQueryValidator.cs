using System;
using FluentValidation;

namespace RecruitIQ.Application.Features.Jobs.GetJobById;

public class GetJobByIdQueryValidator : AbstractValidator<GetJobByIdQuery>
{
    public GetJobByIdQueryValidator()
    {
        RuleFor(x => x.JobId)
            .NotEmpty().WithMessage("Job ID is required.");
    }
}
