using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using RecruitIQ.Application.Common.Interfaces;
using RecruitIQ.Common;
using RecruitIQ.Domain.Entities;

namespace RecruitIQ.Application.Features.Authentication.InviteUser;

public class InviteUserCommandHandler : IRequestHandler<InviteUserCommand, Result<Guid>>
{
    private readonly IRecruitIQDbContext _context;
    private readonly ITenantService _tenantService;
    private readonly IPasswordHasher _passwordHasher;

    public InviteUserCommandHandler(IRecruitIQDbContext context, ITenantService tenantService, IPasswordHasher passwordHasher)
    {
        _context = context;
        _tenantService = tenantService;
        _passwordHasher = passwordHasher;
    }

    public async Task<Result<Guid>> Handle(InviteUserCommand request, CancellationToken cancellationToken)
    {
        var companyId = _tenantService.CompanyId;

        // 1. Check duplicate email in company
        if (_context.Users.Any(u => u.CompanyId == companyId && u.Email.ToLower() == request.Email.ToLower()))
        {
            return Result<Guid>.Failure("Email already exists in this tenant.");
        }

        // 2. Create Invited User with temp password
        var tempPassword = Guid.NewGuid().ToString("N").Substring(0, 12);
        var user = new User
        {
            CompanyId = companyId,
            Email = request.Email.ToLower(),
            PasswordHash = _passwordHasher.HashPassword(tempPassword), // Real app sends invitation email with reset link
            FirstName = request.FirstName,
            LastName = request.LastName,
            IsActive = true
        };
        _context.Add(user);
        await _context.SaveChangesAsync(cancellationToken);

        // 3. Assign Roles
        foreach (var roleName in request.Roles)
        {
            var role = _context.Roles.FirstOrDefault(r => r.Name == roleName);
            if (role == null)
            {
                role = new Role { Name = roleName };
                _context.Add(role);
                await _context.SaveChangesAsync(cancellationToken);
            }

            var userRole = new UserRole
            {
                CompanyId = companyId,
                UserId = user.Id,
                RoleId = role.Id
            };
            _context.Add(userRole);
        }

        await _context.SaveChangesAsync(cancellationToken);
        return Result<Guid>.Success(user.Id);
    }
}
