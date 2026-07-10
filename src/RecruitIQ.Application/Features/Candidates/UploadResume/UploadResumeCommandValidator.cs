using System;
using System.IO;
using System.Linq;
using FluentValidation;

namespace RecruitIQ.Application.Features.Candidates.UploadResume;

public class UploadResumeCommandValidator : AbstractValidator<UploadResumeCommand>
{
    private static readonly string[] AllowedExtensions = { ".pdf", ".doc", ".docx" };
    private static readonly string[] AllowedMimeTypes =
    {
        "application/pdf",
        "application/msword",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
    };

    public UploadResumeCommandValidator()
    {
        RuleFor(x => x.CandidateId)
            .NotEmpty().WithMessage("Candidate ID is required.");

        RuleFor(x => x.FileName)
            .NotEmpty().WithMessage("File name is required.")
            .Must(name => AllowedExtensions.Any(ext => name.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
            .WithMessage("Only .pdf, .doc, and .docx files are allowed.");

        RuleFor(x => x.MimeType)
            .NotEmpty().WithMessage("MIME type is required.")
            .Must(mime => AllowedMimeTypes.Contains(mime))
            .WithMessage("Invalid file format. Only PDF and Word documents are supported.");

        RuleFor(x => x.FileSize)
            .ExclusiveBetween(0, 10 * 1024 * 1024)
            .WithMessage("File size must not exceed 10 MB.");

        RuleFor(x => x.FileStream)
            .NotNull().WithMessage("File stream content is required.");
    }
}
