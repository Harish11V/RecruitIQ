using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using RecruitIQ.Application.Common.Interfaces;
using RecruitIQ.Common;
using RecruitIQ.Domain.Entities;

namespace RecruitIQ.Application.Features.Authentication.ForgotPassword;

public class ForgotPasswordCommandHandler : IRequestHandler<ForgotPasswordCommand, Result>
{
    private readonly IRecruitIQDbContext _context;
    private readonly ITenantService _tenantService;

    public ForgotPasswordCommandHandler(IRecruitIQDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<Result> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var companyId = _tenantService.CompanyId;

        // 1. Find user (tenant isolated)
        var user = _context.Users.FirstOrDefault(u => u.CompanyId == companyId && u.Email.ToLower() == request.Email.ToLower());
        if (user == null)
        {
            // Defensive behavior: return success to prevent email discovery attacks
            return Result.Success();
        }

        // 2. Generate random reset token and hash it
        var rawToken = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
        var tokenHash = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(rawToken)); // Simple hash for demo

        var resetToken = new PasswordResetToken
        {
            UserId = user.Id,
            TokenHash = tokenHash,
            ExpiresAt = DateTime.UtcNow.AddMinutes(15)
        };
        _context.Add(resetToken);

        // 3. Activity log
        var activity = new Activity
        {
            CompanyId = companyId,
            UserId = user.Id,
            Action = "Password Reset Requested",
            EntityName = "Users",
            EntityId = user.Id,
            Timestamp = DateTime.UtcNow
        };
        _context.Add(activity);

        await _context.SaveChangesAsync(cancellationToken);

        // Real app: Send email with rawToken
        return Result.Success();
    }
}
