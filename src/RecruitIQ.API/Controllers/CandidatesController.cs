using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RecruitIQ.Application.Features.Candidates.GetCandidates;
using RecruitIQ.Common;
using RecruitIQ.Contracts;

using RecruitIQ.Application.Features.Candidates.GetCandidateById;
using RecruitIQ.Application.Features.Candidates.CreateCandidate;
using RecruitIQ.Application.Features.Candidates.UpdateCandidate;
using RecruitIQ.Application.Features.Candidates.UploadResume;
using RecruitIQ.Application.Features.Candidates.DeleteResume;
using RecruitIQ.Application.Features.Candidates.SetPrimaryResume;
using RecruitIQ.Application.Features.Candidates.GetCandidateTimeline;
using RecruitIQ.Application.Features.Candidates.ChangeCandidateStatus;
using RecruitIQ.Application.Common.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace RecruitIQ.API.Controllers;

[ApiController]
[Route("api/candidates")]
[Authorize(Policy = "RequireRecruiter")]
public class CandidatesController : ControllerBase
{
    private readonly IMediator _mediator;

    public CandidatesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetCandidates(
        [FromQuery] string? search,
        [FromQuery] string? status,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? sortBy = null,
        [FromQuery] string? sortOrder = null)
    {
        var query = new GetCandidatesQuery(search, status, page, pageSize, sortBy, sortOrder);
        var result = await _mediator.Send(query);

        if (!result.IsSuccess)
        {
            return BadRequest(new ApiResponse<PagedResponse<CandidateSummaryResponse>>(
                default!,
                false,
                "Failed to retrieve candidates.",
                new List<string> { result.Error ?? "Unknown error." }));
        }

        return Ok(new ApiResponse<PagedResponse<CandidateSummaryResponse>>(
            result.Value!,
            true,
            "Candidates retrieved successfully."));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetCandidateById(Guid id)
    {
        var query = new GetCandidateByIdQuery(id);
        var result = await _mediator.Send(query);

        if (!result.IsSuccess)
        {
            return NotFound(new ApiResponse<CandidateDetailsResponse>(
                default!,
                false,
                "Candidate not found.",
                new List<string> { result.Error ?? "Unknown error." }));
        }

        return Ok(new ApiResponse<CandidateDetailsResponse>(
            result.Value!,
            true,
            "Candidate retrieved successfully."));
    }

    [HttpPost]
    public async Task<IActionResult> CreateCandidate([FromBody] CreateCandidateRequest request)
    {
        var command = new CreateCandidateCommand(
            request.FirstName,
            request.LastName,
            request.Email,
            request.PhoneNumber,
            request.LinkedInUrl,
            request.Title,
            request.YearsOfExperience);

        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            return BadRequest(new ApiResponse<Guid>(
                default!,
                false,
                "Failed to create candidate.",
                new List<string> { result.Error ?? "Unknown error." }));
        }

        return CreatedAtAction(
            nameof(GetCandidateById),
            new { id = result.Value },
            new ApiResponse<Guid>(result.Value, true, "Candidate created successfully."));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateCandidate(Guid id, [FromBody] UpdateCandidateRequest request)
    {
        var command = new UpdateCandidateCommand(
            id,
            request.FirstName,
            request.LastName,
            request.Email,
            request.PhoneNumber,
            request.LinkedInUrl,
            request.Title,
            request.Status,
            request.YearsOfExperience,
            request.RowVersion);

        try
        {
            var result = await _mediator.Send(command);

            if (!result.IsSuccess)
            {
                return BadRequest(new ApiResponse<object>(
                    null!,
                    false,
                    "Failed to update candidate.",
                    new List<string> { result.Error ?? "Unknown error." }));
            }

            return Ok(new ApiResponse<byte[]>(
                result.Value!,
                true,
                "Candidate updated successfully."));
        }
        catch (DbUpdateConcurrencyException ex)
        {
            return StatusCode(StatusCodes.Status409Conflict, new ApiResponse<object>(
                null!,
                false,
                "A concurrency conflict occurred. The candidate has been modified by another user.",
                new List<string> { ex.Message }));
        }
    }

    [HttpPost("{id:guid}/resume")]
    public async Task<IActionResult> UploadResume(Guid id, IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new ApiResponse<object>(
                null!,
                false,
                "No file was uploaded.",
                new List<string> { "Please provide a valid file to upload." }));
        }

        using var stream = file.OpenReadStream();
        var command = new UploadResumeCommand(
            id,
            file.FileName,
            file.ContentType,
            file.Length,
            stream);

        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            return BadRequest(new ApiResponse<object>(
                null!,
                false,
                "Failed to upload resume.",
                new List<string> { result.Error ?? "Unknown error." }));
        }

        return Ok(new ApiResponse<Guid>(
            result.Value,
            true,
            "Resume uploaded successfully."));
    }

    [HttpDelete("{id:guid}/resume/{resumeId:guid}")]
    public async Task<IActionResult> DeleteResume(Guid id, Guid resumeId)
    {
        var command = new DeleteResumeCommand(id, resumeId);
        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            return BadRequest(new ApiResponse<object>(
                null!,
                false,
                "Failed to delete resume.",
                new List<string> { result.Error ?? "Unknown error." }));
        }

        return Ok(new ApiResponse<bool>(
            true,
            true,
            "Resume deleted successfully."));
    }

    [HttpPut("{id:guid}/resume/{resumeId:guid}/primary")]
    public async Task<IActionResult> SetPrimaryResume(Guid id, Guid resumeId)
    {
        var command = new SetPrimaryResumeCommand(id, resumeId);
        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            return BadRequest(new ApiResponse<object>(
                null!,
                false,
                "Failed to set primary resume.",
                new List<string> { result.Error ?? "Unknown error." }));
        }

        return Ok(new ApiResponse<bool>(
            true,
            true,
            "Primary resume updated successfully."));
    }

    [HttpGet("{id:guid}/resume/{resumeId:guid}/download")]
    public async Task<IActionResult> DownloadResume(
        Guid id, 
        Guid resumeId, 
        [FromServices] IRecruitIQDbContext context, 
        [FromServices] ITenantService tenantService, 
        [FromServices] IWebHostEnvironment webHostEnvironment)
    {
        var companyId = tenantService.CompanyId;
        var resume = await context.Resumes
            .FirstOrDefaultAsync(r => r.Id == resumeId && r.CandidateId == id && r.CompanyId == companyId && !r.IsDeleted);

        if (resume == null)
        {
            return NotFound(new ApiResponse<object>(
                null!,
                false,
                "Resume not found.",
                new List<string> { "The requested resume does not exist or access was denied." }));
        }

        var normalizedUrl = resume.StoragePath.Replace("/", Path.DirectorySeparatorChar.ToString()).TrimStart(Path.DirectorySeparatorChar);
        var relativePath = Path.Combine("wwwroot", normalizedUrl);
        var contentRoot = webHostEnvironment.ContentRootPath ?? AppContext.BaseDirectory;
        var physicalPath = Path.GetFullPath(Path.Combine(contentRoot, relativePath));

        if (!System.IO.File.Exists(physicalPath))
        {
            return NotFound(new ApiResponse<object>(
                null!,
                false,
                "Physical file missing.",
                new List<string> { "The resume file was not found in the storage directory." }));
        }

        var fileBytes = await System.IO.File.ReadAllBytesAsync(physicalPath);
        return File(fileBytes, resume.MimeType, resume.OriginalFileName);
    }

    [HttpGet("{id:guid}/timeline")]
    public async Task<IActionResult> GetCandidateTimeline(Guid id)
    {
        var query = new GetCandidateTimelineQuery(id);
        var result = await _mediator.Send(query);

        if (!result.IsSuccess)
        {
            return BadRequest(new ApiResponse<object>(
                null!,
                false,
                "Failed to retrieve candidate timeline.",
                new List<string> { result.Error ?? "Unknown error." }));
        }

        return Ok(new ApiResponse<IReadOnlyList<CandidateTimelineItemResponse>>(
            result.Value!,
            true,
            "Candidate timeline retrieved successfully."));
    }

    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> ChangeCandidateStatus(Guid id, [FromBody] ChangeCandidateStatusRequest request)
    {
        try
        {
            var command = new ChangeCandidateStatusCommand(id, request.Status, request.RowVersion);
            var result = await _mediator.Send(command);

            if (!result.IsSuccess)
            {
                if (result.Error == "CandidateNotFound")
                {
                    return NotFound(new ApiResponse<object>(
                        null!,
                        false,
                        "Candidate not found.",
                        new List<string> { "The specified candidate profile was not found." }));
                }

                return BadRequest(new ApiResponse<object>(
                    null!,
                    false,
                    "Failed to change candidate status.",
                    new List<string> { result.Error ?? "Unknown error." }));
            }

            return Ok(new ApiResponse<byte[]>(
                result.Value!,
                true,
                "Candidate status updated successfully."));
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new ApiResponse<object>(
                null!,
                false,
                "ConcurrencyConflict",
                new List<string> { "The candidate record was modified by another user. Please refresh." }));
        }
    }
}
