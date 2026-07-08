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
using RecruitIQ.Application.Features.Jobs.PublishJob;
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

public class PublishJobTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ServiceProvider _serviceProvider;
    private readonly TestTenantService _tenantService;
    private readonly TestCurrentUserService _currentUserService;

    public PublishJobTests()
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
        JobStatus status = JobStatus.Draft,
        DateTime? publishedAt = null,
        bool addSkills = true,
        DateTime? closingDate = null,
        bool hasTitle = true)
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
            Title = hasTitle ? "Software Engineer Backend" : string.Empty,
            JobCode = "JOB-2026-0001",
            Description = "Description",
            Requirements = "Requirements",
            Location = "New York",
            EmploymentType = EmploymentType.FullTime,
            Status = status,
            HiringManagerId = userId,
            ClosingDate = closingDate ?? DateTime.UtcNow.AddDays(10),
            PublishedAt = publishedAt,
            Slug = "software-engineer-backend-0001"
        };
        context.Jobs.Add(job);

        if (addSkills)
        {
            var jobSkill = new JobSkill { JobId = jobId, SkillId = skillId, CompanyId = companyId };
            context.JobSkills.Add(jobSkill);
        }

        await context.SaveChangesAsync();
    }

    private string GenerateTokenForUser(User user, string[] roles)
    {
        using var scope = _serviceProvider.CreateScope();
        var generator = scope.ServiceProvider.GetRequiredService<IJwtTokenGenerator>();
        return generator.GenerateToken(user, roles);
    }

    [Fact]
    public async Task PublishJob_Should_Succeed_With_Valid_Draft()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var deptId = Guid.NewGuid();
        var jobId = Guid.NewGuid();
        var skillId = Guid.NewGuid();
        await SeedDataAsync(companyId, userId, deptId, jobId, skillId);

        _tenantService.CompanyId = companyId;
        _currentUserService.UserId = userId.ToString();

        byte[] rowVersion;
        using (var scope = _serviceProvider.CreateScope())
        {
            rowVersion = scope.ServiceProvider.GetRequiredService<RecruitIQDbContext>().Jobs.First(j => j.Id == jobId).RowVersion;
        }

        var command = new PublishJobCommand(jobId, rowVersion);

        // Act
        var result = await SendAsync(command);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(jobId, result.Value);

        using (var scope = _serviceProvider.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<RecruitIQDbContext>();
            var job = context.Jobs.First(j => j.Id == jobId);
            Assert.Equal(JobStatus.Published, job.Status);
            Assert.NotNull(job.PublishedAt);
            Assert.True((DateTime.UtcNow - job.PublishedAt.Value).TotalMinutes < 1);

            // Activity Log verification
            var activity = context.Activities.FirstOrDefault(a => a.EntityId == jobId);
            Assert.NotNull(activity);
            Assert.Contains("Job Published", activity.Action);
            Assert.Contains("JOB-2026-0001", activity.Action);
            Assert.Contains("Engineering", activity.Action);
        }
    }

    [Fact]
    public async Task PublishJob_Should_Fail_When_Already_Published()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var deptId = Guid.NewGuid();
        var jobId = Guid.NewGuid();
        var skillId = Guid.NewGuid();
        await SeedDataAsync(companyId, userId, deptId, jobId, skillId, JobStatus.Published);

        _tenantService.CompanyId = companyId;

        byte[] rowVersion;
        using (var scope = _serviceProvider.CreateScope())
        {
            rowVersion = scope.ServiceProvider.GetRequiredService<RecruitIQDbContext>().Jobs.First(j => j.Id == jobId).RowVersion;
        }

        var command = new PublishJobCommand(jobId, rowVersion);

        // Act
        var result = await SendAsync(command);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Job is already published.", result.Error);
    }

    [Fact]
    public async Task PublishJob_Should_Fail_When_Archived()
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

        var command = new PublishJobCommand(jobId, rowVersion);

        // Act
        var result = await SendAsync(command);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Archived jobs cannot be published.", result.Error);
    }

    [Fact]
    public async Task PublishJob_Should_Fail_When_Closed()
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

        var command = new PublishJobCommand(jobId, rowVersion);

        // Act
        var result = await SendAsync(command);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Closed jobs cannot be published.", result.Error);
    }

    [Fact]
    public async Task PublishJob_Should_Fail_When_Skills_Are_Missing()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var deptId = Guid.NewGuid();
        var jobId = Guid.NewGuid();
        var skillId = Guid.NewGuid();
        await SeedDataAsync(companyId, userId, deptId, jobId, skillId, JobStatus.Draft, addSkills: false);

        _tenantService.CompanyId = companyId;

        byte[] rowVersion;
        using (var scope = _serviceProvider.CreateScope())
        {
            rowVersion = scope.ServiceProvider.GetRequiredService<RecruitIQDbContext>().Jobs.First(j => j.Id == jobId).RowVersion;
        }

        var command = new PublishJobCommand(jobId, rowVersion);

        // Act
        var result = await SendAsync(command);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("A job must have at least one required skill to be published.", result.Error);
    }

    [Fact]
    public async Task PublishJob_Should_Fail_When_ClosingDate_Is_In_Past()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var deptId = Guid.NewGuid();
        var jobId = Guid.NewGuid();
        var skillId = Guid.NewGuid();
        await SeedDataAsync(companyId, userId, deptId, jobId, skillId, JobStatus.Draft, closingDate: DateTime.UtcNow.AddDays(-2));

        _tenantService.CompanyId = companyId;

        byte[] rowVersion;
        using (var scope = _serviceProvider.CreateScope())
        {
            rowVersion = scope.ServiceProvider.GetRequiredService<RecruitIQDbContext>().Jobs.First(j => j.Id == jobId).RowVersion;
        }

        var command = new PublishJobCommand(jobId, rowVersion);

        // Act
        var result = await SendAsync(command);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Closing date cannot be in the past.", result.Error);
    }

    [Fact]
    public async Task PublishJob_Should_Fail_When_Department_Is_Deleted()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var deptId = Guid.NewGuid();
        var jobId = Guid.NewGuid();
        var skillId = Guid.NewGuid();
        await SeedDataAsync(companyId, userId, deptId, jobId, skillId);

        _tenantService.CompanyId = companyId;

        // Manually delete department
        using (var scope = _serviceProvider.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<RecruitIQDbContext>();
            var dept = context.Departments.First(d => d.Id == deptId);
            context.Departments.Remove(dept);
            await context.SaveChangesAsync();
        }

        byte[] rowVersion;
        using (var scope = _serviceProvider.CreateScope())
        {
            rowVersion = scope.ServiceProvider.GetRequiredService<RecruitIQDbContext>().Jobs.First(j => j.Id == jobId).RowVersion;
        }

        var command = new PublishJobCommand(jobId, rowVersion);

        // Act
        var result = await SendAsync(command);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Department does not exist or has been deleted.", result.Error);
    }

    [Fact]
    public async Task PublishJob_Should_Preserve_Original_PublishedAt()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var deptId = Guid.NewGuid();
        var jobId = Guid.NewGuid();
        var skillId = Guid.NewGuid();
        var originalPublishedAt = DateTime.UtcNow.AddDays(-5);
        await SeedDataAsync(companyId, userId, deptId, jobId, skillId, JobStatus.Draft, originalPublishedAt);

        _tenantService.CompanyId = companyId;

        byte[] rowVersion;
        using (var scope = _serviceProvider.CreateScope())
        {
            rowVersion = scope.ServiceProvider.GetRequiredService<RecruitIQDbContext>().Jobs.First(j => j.Id == jobId).RowVersion;
        }

        var command = new PublishJobCommand(jobId, rowVersion);

        // Act
        var result = await SendAsync(command);

        // Assert
        Assert.True(result.IsSuccess);

        using (var scope = _serviceProvider.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<RecruitIQDbContext>();
            var job = context.Jobs.First(j => j.Id == jobId);
            Assert.Equal(originalPublishedAt.Date, job.PublishedAt.Value.Date); // Preserved!
        }
    }

    [Fact]
    public async Task PublishJob_Endpoint_Should_Enforce_Idempotency()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var deptId = Guid.NewGuid();
        var jobId = Guid.NewGuid();
        var skillId = Guid.NewGuid();
        await SeedDataAsync(companyId, userId, deptId, jobId, skillId);

        var user = new User { Id = userId, CompanyId = companyId, Email = "recruiter@test.com", FirstName = "Rec", LastName = "Ruit", IsActive = true };
        var token = GenerateTokenForUser(user, new[] { "Recruiter" });

        _tenantService.CompanyId = companyId;

        byte[] rowVersion;
        using (var scope = _serviceProvider.CreateScope())
        {
            rowVersion = scope.ServiceProvider.GetRequiredService<RecruitIQDbContext>().Jobs.First(j => j.Id == jobId).RowVersion;
        }

        using var factory = new CustomWebApplicationFactory(_connection);
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Add("X-Tenant", $"test-{companyId:N}");

        var request = new PublishJobRequest(rowVersion);

        // Act & Assert 1: First request succeeds
        var response1 = await client.PostAsJsonAsync($"/api/jobs/{jobId}/publish", request);
        Assert.Equal(HttpStatusCode.OK, response1.StatusCode);

        // Act & Assert 2: Second request fails with BadRequest & Business Error
        var response2 = await client.PostAsJsonAsync($"/api/jobs/{jobId}/publish", request);
        Assert.Equal(HttpStatusCode.BadRequest, response2.StatusCode);
        var errorResponse = await response2.Content.ReadFromJsonAsync<ApiResponse<Guid>>();
        Assert.NotNull(errorResponse);
        Assert.False(errorResponse.Success);
        Assert.Equal("Job is already published.", errorResponse.Message);
    }
}
