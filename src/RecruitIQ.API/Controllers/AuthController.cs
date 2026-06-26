using System.Threading.Tasks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using RecruitIQ.Application.Features.Authentication.CreateCompany;
using RecruitIQ.Application.Features.Authentication.ForgotPassword;
using RecruitIQ.Application.Features.Authentication.InviteUser;
using RecruitIQ.Application.Features.Authentication.Login;
using RecruitIQ.Application.Features.Authentication.Logout;
using RecruitIQ.Application.Features.Authentication.RefreshToken;
using RecruitIQ.Application.Features.Authentication.ResetPassword;
using RecruitIQ.Common;
using RecruitIQ.Contracts;
using System.Collections.Generic;

namespace RecruitIQ.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] CreateCompanyRequest request)
    {
        var command = new CreateCompanyCommand(
            request.Email,
            request.Password,
            request.FirstName,
            request.LastName,
            request.CompanyName,
            request.Subdomain);

        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            return BadRequest(new ApiResponse<System.Guid>(default, false, "Registration failed.", new List<string> { result.Error ?? "Unknown error." }));
        }

        return Ok(new ApiResponse<System.Guid>(result.Value, true, "Registration succeeded."));
    }

    [Authorize(Policy = "RequireCompanyAdmin")]
    [HttpPost("invite")]
    public async Task<IActionResult> Invite([FromBody] InviteUserRequest request)
    {
        var command = new InviteUserCommand(
            request.Email,
            request.FirstName,
            request.LastName,
            request.Roles);

        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            return BadRequest(new ApiResponse<System.Guid>(default, false, "Invitation failed.", new List<string> { result.Error ?? "Unknown error." }));
        }

        return Ok(new ApiResponse<System.Guid>(result.Value, true, "User invited successfully."));
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var userAgent = Request.Headers["User-Agent"].ToString() ?? "unknown";

        var command = new LoginCommand(
            request.Email,
            request.Password,
            ipAddress,
            userAgent);

        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            if (result.Error?.Contains("locked") == true)
            {
                return StatusCode(StatusCodes.Status423Locked, new ApiResponse<AuthResponse>(default!, false, "Account locked.", new List<string> { result.Error }));
            }
            return Unauthorized(new ApiResponse<AuthResponse>(default!, false, "Authentication failed.", new List<string> { result.Error ?? "Invalid credentials." }));
        }

        return Ok(new ApiResponse<AuthResponse>(result.Value!, true, "Login succeeded."));
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        var command = new RefreshTokenCommand(
            request.AccessToken,
            request.RefreshToken,
            ipAddress);

        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            return BadRequest(new ApiResponse<AuthResponse>(default!, false, "Token refresh failed.", new List<string> { result.Error ?? "Unknown error." }));
        }

        return Ok(new ApiResponse<AuthResponse>(result.Value!, true, "Token refreshed successfully."));
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout([FromBody] LogoutRequest request)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

        var command = new LogoutCommand(request.RefreshToken, ipAddress);
        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            return BadRequest(new ApiResponse<object>(null!, false, "Logout failed.", new List<string> { result.Error ?? "Unknown error." }));
        }

        return Ok(new ApiResponse<object>(null!, true, "Logged out successfully."));
    }

    [AllowAnonymous]
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        var command = new ForgotPasswordCommand(request.Email);
        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            return BadRequest(new ApiResponse<object>(null!, false, "Forgot password request failed.", new List<string> { result.Error ?? "Unknown error." }));
        }

        return Ok(new ApiResponse<object>(null!, true, "Password reset email sent if the account exists."));
    }

    [AllowAnonymous]
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        var command = new ResetPasswordCommand(request.Email, request.ResetToken, request.NewPassword);
        var result = await _mediator.Send(command);

        if (!result.IsSuccess)
        {
            return BadRequest(new ApiResponse<object>(null!, false, "Password reset failed.", new List<string> { result.Error ?? "Unknown error." }));
        }

        return Ok(new ApiResponse<object>(null!, true, "Password has been reset successfully."));
    }
}
