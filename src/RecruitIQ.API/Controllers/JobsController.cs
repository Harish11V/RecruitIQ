using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RecruitIQ.Application.Features.Jobs.CreateJob;
using RecruitIQ.Application.Features.Jobs.GetJobById;
using RecruitIQ.Application.Features.Jobs.GetJobs;
using RecruitIQ.Application.Features.Jobs.UpdateJob;
using RecruitIQ.Application.Features.Jobs.PublishJob;
using RecruitIQ.Application.Features.Jobs.ArchiveJob;
using RecruitIQ.Application.Features.Jobs.DeleteJob;
using RecruitIQ.Common;
using RecruitIQ.Contracts;

namespace RecruitIQ.API.Controllers;

[ApiController]
[Route("api/jobs")]
public class JobsController : ControllerBase
{
    private readonly IMediator _mediator;

    public JobsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    [Authorize(Policy = "RequireRecruiter")]
    public async Task<IActionResult> Create([FromBody] CreateJobRequest request)
    {
        var command = new CreateJobCommand(
            request.Title,
            request.Description,
            request.Requirements,
            request.Location,
            request.EmploymentType,
            request.SalaryMin,
            request.SalaryMax,
            request.HiringManagerId,
            request.DepartmentId,
            request.ClosingDate,
            request.RequiredSkills ?? new List<Guid>());

        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            return BadRequest(new ApiResponse<Guid>(
                default!,
                false,
                "Failed to create job.",
                result.Errors));
        }

        return Ok(new ApiResponse<Guid>(
            result.Value,
            true,
            "Job created successfully."));
    }

    [HttpGet]
    [Authorize(Policy = "RequireRecruiter")]
    public async Task<IActionResult> GetJobs([FromQuery] GetJobsRequest request)
    {
        var query = new GetJobsQuery(
            request.Page,
            request.PageSize,
            request.Search,
            request.SortBy,
            request.Status,
            request.DepartmentId,
            request.EmploymentType,
            request.HiringManagerId,
            request.Location);

        var result = await _mediator.Send(query);

        if (!result.IsSuccess)
        {
            return BadRequest(new ApiResponse<PagedResponse<JobSummaryResponse>>(
                default!,
                false,
                "Failed to retrieve jobs.",
                result.Errors));
        }

        return Ok(new ApiResponse<PagedResponse<JobSummaryResponse>>(
            result.Value!,
            true,
            "Jobs retrieved successfully."));
    }

    [HttpGet("{id}")]
    [Authorize(Policy = "RequireRecruiter")]
    public async Task<IActionResult> GetJobById(Guid id)
    {
        var query = new GetJobByIdQuery(id);
        var result = await _mediator.Send(query);

        if (!result.IsSuccess)
        {
            if (result.Error == "Job not found.")
            {
                return NotFound(new ApiResponse<JobDetailsResponse>(
                    default!,
                    false,
                    "Job not found.",
                    result.Errors));
            }

            return BadRequest(new ApiResponse<JobDetailsResponse>(
                default!,
                false,
                "Failed to retrieve job details.",
                result.Errors));
        }

        return Ok(new ApiResponse<JobDetailsResponse>(
            result.Value!,
            true,
            "Job details retrieved successfully."));
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "RequireRecruiter")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateJobRequest request)
    {
        var command = new UpdateJobCommand(
            id,
            request.Title,
            request.Description,
            request.Requirements,
            request.Responsibilities,
            request.DepartmentId,
            request.HiringManagerId,
            request.EmploymentType,
            request.SalaryMin,
            request.SalaryMax,
            request.Location,
            request.ClosingDate,
            request.RequiredSkills,
            request.RowVersion);

        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            if (result.Error == ErrorCodes.JobNotFound)
            {
                return NotFound(new ApiResponse<Guid>(
                    default!,
                    false,
                    "Job not found.",
                    result.Errors));
            }

            if (result.Error == ErrorCodes.ConcurrencyConflict)
            {
                return Conflict(new ApiResponse<Guid>(
                    default!,
                    false,
                    "The job has been modified by another user. Please reload the job and try again."));
            }

            return BadRequest(new ApiResponse<Guid>(
                default!,
                false,
                "Failed to update job.",
                result.Errors));
        }

        return Ok(new ApiResponse<Guid>(
            result.Value,
            true,
            "Job updated successfully."));
    }

    [HttpPost("{id}/publish")]
    [Authorize(Policy = "RequireRecruiter")]
    public async Task<IActionResult> Publish(Guid id, [FromBody] PublishJobRequest request)
    {
        var command = new PublishJobCommand(id, request.RowVersion);
        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            if (result.Error == ErrorCodes.JobNotFound)
            {
                return NotFound(new ApiResponse<Guid>(
                    default!,
                    false,
                    "Job not found.",
                    result.Errors));
            }

            if (result.Error == ErrorCodes.ConcurrencyConflict)
            {
                return Conflict(new ApiResponse<Guid>(
                    default!,
                    false,
                    "The job has been modified by another user. Please reload the job and try again."));
            }

            return BadRequest(new ApiResponse<Guid>(
                default!,
                false,
                result.Error ?? "Failed to publish job.",
                result.Errors));
        }

        return Ok(new ApiResponse<Guid>(
            result.Value,
            true,
            "Job published successfully."));
    }

    [HttpPost("{id}/archive")]
    [Authorize(Policy = "RequireRecruiter")]
    public async Task<IActionResult> Archive(Guid id, [FromBody] ArchiveJobRequest request)
    {
        var command = new ArchiveJobCommand(id, request.RowVersion);
        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            if (result.Error == ErrorCodes.JobNotFound)
            {
                return NotFound(new ApiResponse<Guid>(
                    default!,
                    false,
                    "Job not found.",
                    result.Errors));
            }

            if (result.Error == ErrorCodes.ConcurrencyConflict)
            {
                return Conflict(new ApiResponse<Guid>(
                    default!,
                    false,
                    "The job has been modified by another user. Please reload the job and try again."));
            }

            return BadRequest(new ApiResponse<Guid>(
                default!,
                false,
                result.Error ?? "Failed to archive job.",
                result.Errors));
        }

        return Ok(new ApiResponse<Guid>(
            result.Value,
            true,
            "Job archived successfully."));
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "RequireRecruiter")]
    public async Task<IActionResult> Delete(Guid id, [FromBody] DeleteJobRequest request)
    {
        var command = new DeleteJobCommand(id, request.RowVersion);
        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            if (result.Error == ErrorCodes.JobNotFound)
            {
                return NotFound(new ApiResponse<Guid>(
                    default!,
                    false,
                    "Job not found.",
                    result.Errors));
            }

            if (result.Error == ErrorCodes.ConcurrencyConflict)
            {
                return Conflict(new ApiResponse<Guid>(
                    default!,
                    false,
                    "The job has been modified by another user. Please reload the job and try again."));
            }

            return BadRequest(new ApiResponse<Guid>(
                default!,
                false,
                result.Error ?? "Failed to delete job.",
                result.Errors));
        }

        return Ok(new ApiResponse<Guid>(
            result.Value,
            true,
            "Job deleted successfully."));
    }
}
