using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RecruitIQ.Application;
using RecruitIQ.Application.Common.Interfaces;
using RecruitIQ.Application.Features.Jobs.ArchiveJob;
using RecruitIQ.Common;
using RecruitIQ.Contracts;
using RecruitIQ.Domain.Entities;
using RecruitIQ.Domain.Enums;
using RecruitIQ.Infrastructure;
using RecruitIQ.Infrastructure.Services;
using RecruitIQ.Persistence.DbContext;
using RecruitIQ.Persistence.Interceptors;
using Xunit;

namespace RecruitIQ.IntegrationTests.Jobs;

public class ArchiveJobTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ServiceProvider _serviceProvider;
    private readonly TestTenantService _tenantService;
    private readonly TestCurrentUserService _currentUserService;

    public ArchiveJobTests()
    {
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();

        var services = new ServiceCollection();
        services.AddLogging();

        _tenantService = new TestTenantService();
        _currentUserService = new TestCurrentUserService();

        services.AddSingleton<ITenantService>(_tenantService);
        services.AddSingleton<ICurrentUserService>(_currentUserService);

        services.AddScoped<AuditEntityInterceptor>();
        services.AddScoped<SoftDeleteInterceptor>();

        services.AddDbContext<RecruitIQDbContext>((sp, options) =>
        {
            var auditInterceptor = sp.GetRequiredService<AuditEntityInterceptor>();
            var softDeleteInterceptor = sp.GetRequiredService<SoftDeleteInterceptor>();

            options.UseSqlite(_connection)
                   .AddInterceptors(auditInterceptor, softDeleteInterceptor);
        });

        services.AddScoped<IRecruitIQDbContext>(provider => provider.GetRequiredService<RecruitIQDbContext>());
        services.AddApplicationServices();

        services.Configure<JwtSettings>(options =>
        {
            options.Secret = "super_secret_key_1234567890123456_recruit_iq_tests";
            options.Issuer = "RecruitIQTests";
            options.Audience = "RecruitIQTests";
            options.ExpiryInMinutes = 60;
        });
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

        _serviceProvider = services.BuildServiceProvider();

        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<RecruitIQDbContext>();
        context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _serviceProvider.Dispose();
        _connection.Dispose();
    }

    private async Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request)
    {
        using var scope = _serviceProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        return await mediator.Send(request);
    }

    private async Task SeedDataAsync(
        Guid companyId,
        Guid userId,
        Guid deptId,
        Guid jobId,
        Guid skillId,
        JobStatus status = JobStatus.Published,
        DateTime? publishedAt = null)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<RecruitIQDbContext>();

        var company = new Company { Id = companyId, Name = "Test Company", Subdomain = $"test-{companyId:N}", IsActive = true };
        context.Companies.Add(company);

        var user = new User { Id = userId, CompanyId = companyId, Email = "test@user.com", FirstName = "John", LastName = "Doe", PasswordHash = "hash", IsActive = true };
        context.Users.Add(user);

        var department = new Department { Id = deptId, CompanyId = companyId, Name = "Engineering", Description = "Eng" };
        context.Departments.Add(department);

        var skill = new Skill { Id = skillId, Name = $"Skill-{skillId:N}" };
        context.Skills.Add(skill);

        var job = new Job
        {
            Id = jobId,
            CompanyId = companyId,
            DepartmentId = deptId,
            Title = "Software Engineer Backend",
            JobCode = "JOB-2026-0001",
            Description = "Description",
            Requirements = "Requirements",
            Location = "New York",
            EmploymentType = EmploymentType.FullTime,
            Status = status,
            HiringManagerId = userId,
            ClosingDate = DateTime.UtcNow.AddDays(10),
            PublishedAt = publishedAt ?? DateTime.UtcNow.AddDays(-2),
            Slug = "software-engineer-backend-0001"
        };
        context.Jobs.Add(job);

        var jobSkill = new JobSkill { JobId = jobId, SkillId = skillId, CompanyId = companyId };
        context.JobSkills.Add(jobSkill);

        await context.SaveChangesAsync();
    }

    private string GenerateTokenForUser(User user, string[] roles)
    {
        using var scope = _serviceProvider.CreateScope();
        var generator = scope.ServiceProvider.GetRequiredService<IJwtTokenGenerator>();
        return generator.GenerateToken(user, roles);
    }

    [Fact]
    public async Task ArchiveJob_Should_Succeed_With_Published_Job()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var deptId = Guid.NewGuid();
        var jobId = Guid.NewGuid();
        var skillId = Guid.NewGuid();
        var publishedDate = DateTime.UtcNow.AddDays(-3);
        await SeedDataAsync(companyId, userId, deptId, jobId, skillId, JobStatus.Published, publishedDate);

        _tenantService.CompanyId = companyId;
        _currentUserService.UserId = userId.ToString();

        byte[] rowVersion;
        using (var scope = _serviceProvider.CreateScope())
        {
            rowVersion = scope.ServiceProvider.GetRequiredService<RecruitIQDbContext>().Jobs.First(j => j.Id == jobId).RowVersion;
        }

        var command = new ArchiveJobCommand(jobId, rowVersion);

        // Act
        var result = await SendAsync(command);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(jobId, result.Value);

        using (var scope = _serviceProvider.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<RecruitIQDbContext>();
            var job = context.Jobs.First(j => j.Id == jobId);
            Assert.Equal(JobStatus.Archived, job.Status);
            Assert.Equal(publishedDate.Date, job.PublishedAt.Value.Date); // Preserved!

            // Activity Log verification
            var activity = context.Activities.FirstOrDefault(a => a.EntityId == jobId);
            Assert.NotNull(activity);
            Assert.Contains("Job Archived", activity.Action);
            Assert.Contains("JOB-2026-0001", activity.Action);
            Assert.Contains("Engineering", activity.Action);
        }
    }

    [Fact]
    public async Task ArchiveJob_Should_Fail_When_Draft()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var deptId = Guid.NewGuid();
        var jobId = Guid.NewGuid();
        var skillId = Guid.NewGuid();
        await SeedDataAsync(companyId, userId, deptId, jobId, skillId, JobStatus.Draft);

        _tenantService.CompanyId = companyId;

        byte[] rowVersion;
        using (var scope = _serviceProvider.CreateScope())
        {
            rowVersion = scope.ServiceProvider.GetRequiredService<RecruitIQDbContext>().Jobs.First(j => j.Id == jobId).RowVersion;
        }

        var command = new ArchiveJobCommand(jobId, rowVersion);

        // Act
        var result = await SendAsync(command);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Draft jobs cannot be archived.", result.Error);
    }

    [Fact]
    public async Task ArchiveJob_Should_Fail_When_Closed()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var deptId = Guid.NewGuid();
        var jobId = Guid.NewGuid();
        var skillId = Guid.NewGuid();
        await SeedDataAsync(companyId, userId, deptId, jobId, skillId, JobStatus.Closed);

        _tenantService.CompanyId = companyId;

        byte[] rowVersion;
        using (var scope = _serviceProvider.CreateScope())
        {
            rowVersion = scope.ServiceProvider.GetRequiredService<RecruitIQDbContext>().Jobs.First(j => j.Id == jobId).RowVersion;
        }

        var command = new ArchiveJobCommand(jobId, rowVersion);

        // Act
        var result = await SendAsync(command);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Closed jobs cannot be archived.", result.Error);
    }

    [Fact]
    public async Task ArchiveJob_Should_Fail_When_Already_Archived()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var deptId = Guid.NewGuid();
        var jobId = Guid.NewGuid();
        var skillId = Guid.NewGuid();
        await SeedDataAsync(companyId, userId, deptId, jobId, skillId, JobStatus.Archived);

        _tenantService.CompanyId = companyId;

        byte[] rowVersion;
        using (var scope = _serviceProvider.CreateScope())
        {
            rowVersion = scope.ServiceProvider.GetRequiredService<RecruitIQDbContext>().Jobs.First(j => j.Id == jobId).RowVersion;
        }

        var command = new ArchiveJobCommand(jobId, rowVersion);

        // Act
        var result = await SendAsync(command);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Job is already archived.", result.Error);
    }

    [Fact]
    public async Task ArchiveJob_Should_Enforce_Tenant_Isolation()
    {
        // Arrange
        var company1 = Guid.NewGuid();
        var user1 = Guid.NewGuid();
        var dept1 = Guid.NewGuid();
        var job1 = Guid.NewGuid();
        await SeedDataAsync(company1, user1, dept1, job1, Guid.NewGuid());

        var company2 = Guid.NewGuid();
        var user2 = Guid.NewGuid();
        var dept2 = Guid.NewGuid();
        var job2 = Guid.NewGuid();
        await SeedDataAsync(company2, user2, dept2, job2, Guid.NewGuid());

        _tenantService.CompanyId = company1;

        byte[] rowVersion;
        using (var scope = _serviceProvider.CreateScope())
        {
            rowVersion = scope.ServiceProvider.GetRequiredService<RecruitIQDbContext>().Jobs.First(j => j.Id == job1).RowVersion;
        }

        // Try to archive job2 belonging to company2 using company1 tenant context
        var command = new ArchiveJobCommand(job2, rowVersion);

        // Act
        var result = await SendAsync(command);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.JobNotFound, result.Error);
    }

    [Fact]
    public async Task ArchiveJob_Endpoint_Should_Return_Conflict_When_Concurrency_Mismatch()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var deptId = Guid.NewGuid();
        var jobId = Guid.NewGuid();
        await SeedDataAsync(companyId, userId, deptId, jobId, Guid.NewGuid());

        var user = new User { Id = userId, CompanyId = companyId, Email = "recruiter@test.com", FirstName = "Rec", LastName = "Ruit", IsActive = true };
        var token = GenerateTokenForUser(user, new[] { "Recruiter" });

        using var factory = new CustomWebApplicationFactory(_connection);
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Add("X-Tenant", $"test-{companyId:N}");

        var request = new ArchiveJobRequest(new byte[] { 1, 1, 1, 1, 1, 1, 1, 1 }); // Stale version

        // Act
        var response = await client.PostAsJsonAsync($"/api/jobs/{jobId}/archive", request);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}
