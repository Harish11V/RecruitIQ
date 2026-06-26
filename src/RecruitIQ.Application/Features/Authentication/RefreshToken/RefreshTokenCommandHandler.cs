using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using RecruitIQ.Application.Common.Interfaces;
using RecruitIQ.Common;
using RecruitIQ.Contracts;
using RecruitIQ.Domain.Entities;

namespace RecruitIQ.Application.Features.Authentication.RefreshToken;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, Result<AuthResponse>>
{
    private readonly IRecruitIQDbContext _context;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;

    public RefreshTokenCommandHandler(IRecruitIQDbContext context, IJwtTokenGenerator jwtTokenGenerator)
    {
        _context = context;
        _jwtTokenGenerator = jwtTokenGenerator;
    }

    public async Task<Result<AuthResponse>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        // 1. Get principal
        var principal = _jwtTokenGenerator.GetPrincipalFromExpiredToken(request.AccessToken);
        if (principal == null)
        {
            return Result<AuthResponse>.Failure("Invalid access token.");
        }

        var userIdClaim = principal.Claims.FirstOrDefault(c => c.Type == "sub" || c.Type == System.Security.Claims.ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !Guid.TryParse(userIdClaim.Value, out var userId))
        {
            return Result<AuthResponse>.Failure("Invalid access token claims.");
        }

        // 2. Retrieve refresh token
        var refreshToken = _context.RefreshTokens.FirstOrDefault(rt => rt.Token == request.RefreshToken);
        if (refreshToken == null || refreshToken.UserId != userId)
        {
            return Result<AuthResponse>.Failure("Invalid refresh token.");
        }

        // 3. Expiration/Revocation check
        if (refreshToken.ExpiresAt < DateTime.UtcNow)
        {
            return Result<AuthResponse>.Failure("Refresh token has expired.");
        }

        if (refreshToken.RevokedAt.HasValue)
        {
            return Result<AuthResponse>.Failure("Refresh token has been revoked.");
        }

        // 4. Generate new tokens
        var user = _context.Users.FirstOrDefault(u => u.Id == userId);
        if (user == null)
        {
            return Result<AuthResponse>.Failure("User not found.");
        }

        var roles = _context.UserRoles
            .Where(ur => ur.UserId == userId)
            .Select(ur => ur.Role.Name)
            .ToList();

        var newJwt = _jwtTokenGenerator.GenerateToken(user, roles);
        var newRefreshTokenStr = _jwtTokenGenerator.GenerateRefreshToken();

        // 5. Rotate token (revoke old)
        refreshToken.RevokedAt = DateTime.UtcNow;
        refreshToken.RevokedByIp = request.IpAddress;
        refreshToken.ReplacedByToken = newRefreshTokenStr;

        var newRefreshToken = new RecruitIQ.Domain.Entities.RefreshToken
        {
            UserId = user.Id,
            Token = newRefreshTokenStr,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedByIp = request.IpAddress,
            UserAgent = refreshToken.UserAgent
        };
        _context.Add(newRefreshToken);

        await _context.SaveChangesAsync(cancellationToken);

        return Result<AuthResponse>.Success(new AuthResponse(newJwt, newRefreshTokenStr, DateTime.UtcNow.AddMinutes(60)));
    }
}
