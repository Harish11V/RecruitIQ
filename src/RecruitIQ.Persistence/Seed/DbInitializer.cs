using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RecruitIQ.Domain.Entities;
using RecruitIQ.Persistence.DbContext;

namespace RecruitIQ.Persistence.Seed;

public static class DbInitializer
{
    public static async Task InitializeAsync(RecruitIQDbContext context, string environmentName)
    {
        // 1. Auto-migration only in Development
        if (environmentName.Equals("Development", StringComparison.OrdinalIgnoreCase))
        {
            await context.Database.MigrateAsync();
        }

        // 2. Seeding global Roles
        if (!context.Roles.Any())
        {
            var roles = new[]
            {
                new Role { Name = "Company Admin" },
                new Role { Name = "Recruiter" },
                new Role { Name = "Interviewer" }
            };

            context.Roles.AddRange(roles);
            await context.SaveChangesAsync();
        }

        // 3. Seeding default Company and HiringStages
        if (!context.Companies.Any())
        {
            var defaultCompany = new Company
            {
                Name = "Default RecruitIQ Tenant",
                Subdomain = "default",
                IsActive = true
            };

            context.Companies.Add(defaultCompany);
            await context.SaveChangesAsync();

            var stages = new[]
            {
                new HiringStage { CompanyId = defaultCompany.Id, Name = "Applied", SequenceOrder = 1 },
                new HiringStage { CompanyId = defaultCompany.Id, Name = "Screening", SequenceOrder = 2 },
                new HiringStage { CompanyId = defaultCompany.Id, Name = "Technical", SequenceOrder = 3 },
                new HiringStage { CompanyId = defaultCompany.Id, Name = "Manager", SequenceOrder = 4 },
                new HiringStage { CompanyId = defaultCompany.Id, Name = "HR", SequenceOrder = 5 },
                new HiringStage { CompanyId = defaultCompany.Id, Name = "Offer", SequenceOrder = 6 },
                new HiringStage { CompanyId = defaultCompany.Id, Name = "Joined", SequenceOrder = 7 },
                new HiringStage { CompanyId = defaultCompany.Id, Name = "Rejected", SequenceOrder = 8 }
            };

            context.HiringStages.AddRange(stages);
            await context.SaveChangesAsync();
        }
    }
}
