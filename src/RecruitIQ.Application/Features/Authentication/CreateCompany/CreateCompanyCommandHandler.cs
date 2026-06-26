using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using RecruitIQ.Application.Common.Interfaces;
using RecruitIQ.Common;
using RecruitIQ.Domain.Entities;

namespace RecruitIQ.Application.Features.Authentication.CreateCompany;

public class CreateCompanyCommandHandler : IRequestHandler<CreateCompanyCommand, Result<Guid>>
{
    private readonly IRecruitIQDbContext _context;
    private readonly IPasswordHasher _passwordHasher;

    public CreateCompanyCommandHandler(IRecruitIQDbContext context, IPasswordHasher passwordHasher)
    {
        _context = context;
        _passwordHasher = passwordHasher;
    }

    public async Task<Result<Guid>> Handle(CreateCompanyCommand request, CancellationToken cancellationToken)
    {
        // 1. Check duplicate subdomain
        if (_context.Companies.Any(c => c.Subdomain.ToLower() == request.Subdomain.ToLower()))
        {
            return Result<Guid>.Failure("Subdomain is already taken.");
        }

        // 2. Resolve Role
        var role = _context.Roles.FirstOrDefault(r => r.Name == "Company Admin");
        if (role == null)
        {
            role = new Role { Name = "Company Admin" };
            _context.Add(role);
            await _context.SaveChangesAsync(cancellationToken);
        }

        // 3. Create Company
        var company = new Company
        {
            Name = request.CompanyName,
            Subdomain = request.Subdomain.ToLower(),
            IsActive = true
        };
        _context.Add(company);
        await _context.SaveChangesAsync(cancellationToken);

        // 4. Create Settings
        var settings = new CompanySettings
        {
            CompanyId = company.Id,
            Theme = "Light",
            Timezone = "UTC",
            DefaultInterviewDuration = 30
        };
        _context.Add(settings);

        // 5. Create Default Hiring Stages
        var stages = new[]
        {
            new HiringStage { CompanyId = company.Id, Name = "Applied", SequenceOrder = 1 },
            new HiringStage { CompanyId = company.Id, Name = "Screening", SequenceOrder = 2 },
            new HiringStage { CompanyId = company.Id, Name = "Technical", SequenceOrder = 3 },
            new HiringStage { CompanyId = company.Id, Name = "Manager", SequenceOrder = 4 },
            new HiringStage { CompanyId = company.Id, Name = "HR", SequenceOrder = 5 },
            new HiringStage { CompanyId = company.Id, Name = "Offer", SequenceOrder = 6 },
            new HiringStage { CompanyId = company.Id, Name = "Joined", SequenceOrder = 7 },
            new HiringStage { CompanyId = company.Id, Name = "Rejected", SequenceOrder = 8 }
        };
        foreach (var stage in stages)
        {
            _context.Add(stage);
        }

        // 6. Create Admin User
        var admin = new User
        {
            CompanyId = company.Id,
            Email = request.Email.ToLower(),
            PasswordHash = _passwordHasher.HashPassword(request.Password),
            FirstName = request.FirstName,
            LastName = request.LastName,
            IsActive = true
        };
        _context.Add(admin);
        await _context.SaveChangesAsync(cancellationToken);

        // 7. Map UserRole
        var userRole = new UserRole
        {
            CompanyId = company.Id,
            UserId = admin.Id,
            RoleId = role.Id
        };
        _context.Add(userRole);
        await _context.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(company.Id);
    }
}
