using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using RecruitIQ.Application.Common.Interfaces;
using RecruitIQ.Common;
using RecruitIQ.Contracts;
using RecruitIQ.Domain.Entities;

namespace RecruitIQ.Application.Features.Authentication.Login;

public class LoginCommandHandler : IRequestHandler<LoginCommand, Result<AuthResponse>>
{
    private readonly IRecruitIQDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly ITenantService _tenantService;

    public LoginCommandHandler(
        IRecruitIQDbContext context, 
        IPasswordHasher passwordHasher, 
        IJwtTokenGenerator jwtTokenGenerator,
        ITenantService tenantService)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _tenantService = tenantService;
    }

    public async Task<Result<AuthResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var companyId = _tenantService.CompanyId;
        
        // Find User
        var user = _context.Users.FirstOrDefault(u => u.CompanyId == companyId && u.Email.ToLower() == request.Email.ToLower());
        if (user == null)
        {
            return Result<AuthResponse>.Failure("Invalid email or password.");
        }

        // Lockout Check
        if (user.LockoutUntil.HasValue && user.LockoutUntil.Value > DateTime.UtcNow)
        {
            return Result<AuthResponse>.Failure("Account is locked due to multiple failed login attempts. Try again later.");
        }

        // Password Verification
        bool isPasswordValid = _passwordHasher.VerifyPassword(request.Password, user.PasswordHash);
        if (!isPasswordValid)
        {
            user.FailedLoginAttempts++;
            if (user.FailedLoginAttempts >= 5)
            {
                user.LockoutUntil = DateTime.UtcNow.AddMinutes(15);
            }
            await _context.SaveChangesAsync(cancellationToken);
            return Result<AuthResponse>.Failure("Invalid email or password.");
        }

        // Reset failed attempts, set audits
        user.FailedLoginAttempts = 0;
        user.LockoutUntil = null;
        user.LastLoginAt = DateTime.UtcNow;
        user.LastLoginIp = request.IpAddress;

        // Fetch roles
        var roles = _context.UserRoles
            .Where(ur => ur.UserId == user.Id)
            .Select(ur => ur.Role.Name)
            .ToList();

        // Generate Tokens
        var token = _jwtTokenGenerator.GenerateToken(user, roles);
        var refreshTokenStr = _jwtTokenGenerator.GenerateRefreshToken();

        var refreshToken = new RecruitIQ.Domain.Entities.RefreshToken
        {
            UserId = user.Id,
            Token = refreshTokenStr,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedByIp = request.IpAddress,
            UserAgent = request.UserAgent
        };
        _context.Add(refreshToken);

        // Activity log
        var activity = new Activity
        {
            CompanyId = companyId,
            UserId = user.Id,
            Action = "User Logged In",
            EntityName = "Users",
            EntityId = user.Id,
            Timestamp = DateTime.UtcNow
        };
        _context.Add(activity);

        await _context.SaveChangesAsync(cancellationToken);

        return Result<AuthResponse>.Success(new AuthResponse(token, refreshTokenStr, DateTime.UtcNow.AddMinutes(60)));
    }
}
