using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using RecruitIQ.Application.Common.Interfaces;
using RecruitIQ.Common;
using RecruitIQ.Domain.Entities;

namespace RecruitIQ.Application.Features.Authentication.ResetPassword;

public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, Result>
{
    private readonly IRecruitIQDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITenantService _tenantService;

    public ResetPasswordCommandHandler(
        IRecruitIQDbContext context, 
        IPasswordHasher passwordHasher,
        ITenantService tenantService)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _tenantService = tenantService;
    }

    public async Task<Result> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var companyId = _tenantService.CompanyId;
        var tokenHash = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(request.ResetToken));

        // Find active token
        var resetToken = _context.PasswordResetTokens
            .FirstOrDefault(t => t.TokenHash == tokenHash && t.ExpiresAt > DateTime.UtcNow && t.UsedAt == null);

        if (resetToken == null)
        {
            return Result.Failure("Invalid or expired password reset token.");
        }

        var user = _context.Users.FirstOrDefault(u => u.Id == resetToken.UserId && u.CompanyId == companyId && u.Email.ToLower() == request.Email.ToLower());
        if (user == null)
        {
            return Result.Failure("Invalid email or password reset token.");
        }

        // Apply reset
        user.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);
        user.FailedLoginAttempts = 0;
        user.LockoutUntil = null;

        resetToken.UsedAt = DateTime.UtcNow;

        // Activity log
        var activity = new Activity
        {
            CompanyId = companyId,
            UserId = user.Id,
            Action = "Password Changed",
            EntityName = "Users",
            EntityId = user.Id,
            Timestamp = DateTime.UtcNow
        };
        _context.Add(activity);

        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
