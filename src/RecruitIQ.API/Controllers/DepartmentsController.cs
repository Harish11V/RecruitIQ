using System.Collections.Generic;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RecruitIQ.Application.Features.Departments.GetDepartments;
using RecruitIQ.Common;
using RecruitIQ.Contracts;

namespace RecruitIQ.API.Controllers;

[ApiController]
[Route("api/departments")]
public class DepartmentsController : ControllerBase
{
    private readonly IMediator _mediator;

    public DepartmentsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [Authorize(Policy = "RequireRecruiter")]
    public async Task<IActionResult> GetDepartments()
    {
        var query = new GetDepartmentsQuery();
        var result = await _mediator.Send(query);

        if (!result.IsSuccess)
        {
            return BadRequest(new ApiResponse<IReadOnlyList<DepartmentSummaryResponse>>(
                default!,
                false,
                "Failed to retrieve departments.",
                result.Errors));
        }

        return Ok(new ApiResponse<IReadOnlyList<DepartmentSummaryResponse>>(
            result.Value!,
            true,
            "Departments retrieved successfully."));
    }
}
