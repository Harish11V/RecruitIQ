using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RecruitIQ.Application.Features.CompanySettings.GetCompanySettings;
using RecruitIQ.Application.Features.CompanySettings.UpdateCompanySettings;
using RecruitIQ.Application.Features.CompanySettings.UploadCompanyLogo;
using RecruitIQ.Common;
using RecruitIQ.Contracts;

namespace RecruitIQ.API.Controllers;

[ApiController]
[Route("api/company-settings")]
[Authorize(Policy = "RequireCompanyAdmin")]
public class CompanySettingsController : ControllerBase
{
    private readonly IMediator _mediator;

    public CompanySettingsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var query = new GetCompanySettingsQuery();
        var result = await _mediator.Send(query);

        if (!result.IsSuccess)
        {
            return BadRequest(new ApiResponse<CompanySettingsResponse>(default!, false, "Failed to retrieve company settings.", new List<string> { result.Error ?? "Unknown error." }));
        }

        return Ok(new ApiResponse<CompanySettingsResponse>(result.Value!, true, "Company settings retrieved successfully."));
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] UpdateCompanySettingsRequest request)
    {
        var command = new UpdateCompanySettingsCommand(
            request.Theme,
            request.Timezone,
            request.DefaultInterviewDuration,
            request.AllowedEmailDomain);

        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            return BadRequest(new ApiResponse<object>(null!, false, "Failed to update company settings.", new List<string> { result.Error ?? "Unknown error." }));
        }

        return Ok(new ApiResponse<object>(null!, true, "Company settings updated successfully."));
    }

    [HttpPost("logo")]
    public async Task<IActionResult> UploadLogo(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new ApiResponse<string>(default!, false, "Invalid request.", new List<string> { "No file uploaded." }));
        }

        using var stream = file.OpenReadStream();
        var command = new UploadCompanyLogoCommand(stream, file.FileName, file.ContentType);
        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            return BadRequest(new ApiResponse<string>(default!, false, "Failed to upload company logo.", new List<string> { result.Error ?? "Unknown error." }));
        }

        return Ok(new ApiResponse<string>(result.Value!, true, "Company logo uploaded successfully."));
    }
}
