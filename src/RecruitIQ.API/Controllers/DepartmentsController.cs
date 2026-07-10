using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RecruitIQ.Application.Features.Departments.GetDepartments;
using RecruitIQ.Application.Features.Departments.CreateDepartment;
using RecruitIQ.Application.Features.Departments.UpdateDepartment;
using RecruitIQ.Application.Features.Departments.DeleteDepartment;
using RecruitIQ.Common;
using RecruitIQ.Contracts;

namespace RecruitIQ.API.Controllers;

[ApiController]
[Route("api/departments")]
[Authorize(Policy = "RequireRecruiter")]
public class DepartmentsController : ControllerBase
{
    private readonly IMediator _mediator;

    public DepartmentsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetDepartments([FromQuery] string? search)
    {
        var query = new GetDepartmentsQuery(search);
        var result = await _mediator.Send(query);

        if (!result.IsSuccess)
        {
            return BadRequest(new ApiResponse<IReadOnlyList<DepartmentResponse>>(
                default!,
                false,
                "Failed to retrieve departments.",
                new List<string> { result.Error ?? "Unknown error." }));
        }

        return Ok(new ApiResponse<IReadOnlyList<DepartmentResponse>>(
            result.Value!,
            true,
            "Departments retrieved successfully."));
    }

    [HttpPost]
    [Authorize(Policy = "RequireCompanyAdmin")]
    public async Task<IActionResult> Create([FromBody] CreateDepartmentRequest request)
    {
        var command = new CreateDepartmentCommand(request.Name, request.Description);
        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            return BadRequest(new ApiResponse<Guid>(
                default!,
                false,
                "Failed to create department.",
                new List<string> { result.Error ?? "Unknown error." }));
        }

        return Ok(new ApiResponse<Guid>(result.Value, true, "Department created successfully."));
    }

    [HttpPut("{id}")]
    [Authorize(Policy = "RequireCompanyAdmin")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateDepartmentRequest request)
    {
        var command = new UpdateDepartmentCommand(id, request.Name, request.Description, request.RowVersion);
        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            if (result.Error != null && result.Error.Contains("concurrency"))
            {
                return Conflict(new ApiResponse<object>(null!, false, "A concurrency conflict occurred.", new List<string> { result.Error }));
            }
            return BadRequest(new ApiResponse<object>(null!, false, "Failed to update department.", new List<string> { result.Error ?? "Unknown error." }));
        }

        return Ok(new ApiResponse<object>(null!, true, "Department updated successfully."));
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = "RequireCompanyAdmin")]
    public async Task<IActionResult> Delete(Guid id, [FromQuery] string rowVersion)
    {
        byte[] rowVersionBytes;
        try
        {
            rowVersionBytes = Convert.FromBase64String(rowVersion);
        }
        catch
        {
            return BadRequest(new ApiResponse<object>(null!, false, "Invalid rowVersion token structure.", new List<string> { "rowVersion must be a base64 string." }));
        }

        var command = new DeleteDepartmentCommand(id, rowVersionBytes);
        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            if (result.Error != null && result.Error.Contains("concurrency"))
            {
                return Conflict(new ApiResponse<object>(null!, false, "A concurrency conflict occurred.", new List<string> { result.Error }));
            }
            return BadRequest(new ApiResponse<object>(null!, false, "Failed to delete department.", new List<string> { result.Error ?? "Unknown error." }));
        }

        return Ok(new ApiResponse<object>(null!, true, "Department deleted successfully."));
    }
}
