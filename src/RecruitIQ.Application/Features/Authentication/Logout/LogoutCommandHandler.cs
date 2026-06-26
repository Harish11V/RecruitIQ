using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using RecruitIQ.Application.Common.Interfaces;
using RecruitIQ.Common;
using RecruitIQ.Domain.Entities;

namespace RecruitIQ.Application.Features.Authentication.Logout;

public class LogoutCommandHandler : IRequestHandler<LogoutCommand, Result>
{
    private readonly IRecruitIQDbContext _context;

    public LogoutCommandHandler(IRecruitIQDbContext context)
    {
        _context = context;
    }

    public async Task<Result> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var refreshToken = _context.RefreshTokens.FirstOrDefault(rt => rt.Token == request.RefreshToken);
        if (refreshToken == null)
        {
            return Result.Failure("Invalid refresh token.");
        }

        if (refreshToken.RevokedAt.HasValue)
        {
            return Result.Success(); // Idempotent
        }

        // Revoke
        refreshToken.RevokedAt = DateTime.UtcNow;
        refreshToken.RevokedByIp = request.IpAddress;

        // Activity log
        var activity = new Activity
        {
            CompanyId = refreshToken.User.CompanyId,
            UserId = refreshToken.UserId,
            Action = "User Logged Out",
            EntityName = "Users",
            EntityId = refreshToken.UserId,
            Timestamp = DateTime.UtcNow
        };
        _context.Add(activity);

        await _context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
