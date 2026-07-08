using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using RecruitIQ.Application;
using RecruitIQ.Application.Common.Interfaces;
using RecruitIQ.Application.Features.Jobs.UpdateJob;
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

public class UpdateJobTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ServiceProvider _serviceProvider;
    private readonly TestTenantService _tenantService;
    private readonly TestCurrentUserService _currentUserService;

    public UpdateJobTests()
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
            PublishedAt = publishedAt,
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
    public async Task UpdateJob_Should_Succeed_With_Valid_Command()
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

        // Get initial row version and closing date
        byte[] initialRowVersion;
        DateTime? closingDate;
        using (var scope = _serviceProvider.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<RecruitIQDbContext>();
            var seededJob = context.Jobs.First(j => j.Id == jobId);
            initialRowVersion = seededJob.RowVersion;
            closingDate = seededJob.ClosingDate;
        }

        var command = new UpdateJobCommand(
            jobId,
            "Senior Backend Engineer",
            "Description",
            "Requirements",
            "Responsibilities",
            deptId,
            userId,
            EmploymentType.FullTime,
            null,
            null,
            "San Francisco",
            closingDate,
            new List<Guid> { skillId },
            initialRowVersion);

        // Act
        var result = await SendAsync(command);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(jobId, result.Value);

        using (var scope = _serviceProvider.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<RecruitIQDbContext>();
            var job = context.Jobs.First(j => j.Id == jobId);
            Assert.Equal("Senior Backend Engineer", job.Title);
            Assert.Equal("Description", job.Description);
            Assert.Equal("San Francisco", job.Location);
            Assert.Equal(EmploymentType.FullTime, job.EmploymentType);
            Assert.Null(job.SalaryMin);
            Assert.Null(job.SalaryMax);

            // Audit update verification
            Assert.Equal(userId.ToString(), job.UpdatedBy);
            Assert.NotNull(job.UpdatedAt);

            // RowVersion rotation verification
            Assert.False(initialRowVersion.SequenceEqual(job.RowVersion));

            // Activity Log verification
            var activity = context.Activities.FirstOrDefault(a => a.EntityId == jobId);
            Assert.NotNull(activity);
            Assert.Contains("Job Updated", activity.Action);
            Assert.Contains("Title", activity.Action);
            Assert.Contains("Location", activity.Action);
        }
    }

    [Fact]
    public async Task UpdateJob_Should_Fail_When_Department_Does_Not_Exist()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var deptId = Guid.NewGuid();
        var jobId = Guid.NewGuid();
        var skillId = Guid.NewGuid();
        await SeedDataAsync(companyId, userId, deptId, jobId, skillId);

        _tenantService.CompanyId = companyId;

        byte[] rowVersion;
        using (var scope = _serviceProvider.CreateScope())
        {
            rowVersion = scope.ServiceProvider.GetRequiredService<RecruitIQDbContext>().Jobs.First(j => j.Id == jobId).RowVersion;
        }

        var command = new UpdateJobCommand(
            jobId,
            "Title",
            "Description",
            "Requirements",
            "Responsibilities",
            Guid.NewGuid(), // Invalid Department
            userId,
            EmploymentType.FullTime,
            null, null, "Location", null, new List<Guid> { skillId },
            rowVersion);

        // Act
        var result = await SendAsync(command);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Department does not exist or does not belong to the current tenant.", result.Error);
    }

    [Fact]
    public async Task UpdateJob_Should_Fail_When_HiringManager_Belongs_To_Other_Tenant()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var deptId = Guid.NewGuid();
        var jobId = Guid.NewGuid();
        var skillId = Guid.NewGuid();
        await SeedDataAsync(companyId, userId, deptId, jobId, skillId);

        var otherCompanyId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        await SeedDataAsync(otherCompanyId, otherUserId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        _tenantService.CompanyId = companyId;

        byte[] rowVersion;
        using (var scope = _serviceProvider.CreateScope())
        {
            rowVersion = scope.ServiceProvider.GetRequiredService<RecruitIQDbContext>().Jobs.First(j => j.Id == jobId).RowVersion;
        }

        var command = new UpdateJobCommand(
            jobId,
            "Title",
            "Description",
            "Requirements",
            "Responsibilities",
            deptId,
            otherUserId, // HiringManager from other tenant
            EmploymentType.FullTime,
            null, null, "Location", null, new List<Guid> { skillId },
            rowVersion);

        // Act
        var result = await SendAsync(command);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Hiring manager does not exist or does not belong to the current tenant.", result.Error);
    }

    [Fact]
    public async Task UpdateJob_Should_Fail_When_Archived_Jobs_Are_Edited()
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

        var command = new UpdateJobCommand(
            jobId,
            "Title",
            "Description",
            "Requirements",
            "Responsibilities",
            deptId,
            userId,
            EmploymentType.FullTime,
            null, null, "Location", null, new List<Guid> { skillId },
            rowVersion);

        // Act
        var result = await SendAsync(command);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Archived jobs cannot be edited.", result.Error);
    }

    [Fact]
    public async Task UpdateJob_Should_Preserve_PublishedAt_When_Job_Is_Already_Published()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var deptId = Guid.NewGuid();
        var jobId = Guid.NewGuid();
        var skillId = Guid.NewGuid();
        var publishedDate = DateTime.UtcNow.AddDays(-2);
        await SeedDataAsync(companyId, userId, deptId, jobId, skillId, JobStatus.Published, publishedDate);

        _tenantService.CompanyId = companyId;

        byte[] rowVersion;
        using (var scope = _serviceProvider.CreateScope())
        {
            rowVersion = scope.ServiceProvider.GetRequiredService<RecruitIQDbContext>().Jobs.First(j => j.Id == jobId).RowVersion;
        }

        var command = new UpdateJobCommand(
            jobId,
            "Updated Title",
            "Description",
            "Requirements",
            "Responsibilities",
            deptId,
            userId,
            EmploymentType.FullTime,
            null, null, "Location", null, new List<Guid> { skillId },
            rowVersion);

        // Act
        var result = await SendAsync(command);

        // Assert
        Assert.True(result.IsSuccess);

        using (var scope = _serviceProvider.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<RecruitIQDbContext>();
            var job = context.Jobs.First(j => j.Id == jobId);
            Assert.Equal("Updated Title", job.Title);
            Assert.NotNull(job.PublishedAt);
            Assert.Equal(publishedDate.Date, job.PublishedAt.Value.Date); // original timestamp preserved!
        }
    }

    [Fact]
    public async Task UpdateJob_Should_Fail_When_RowVersion_Conflict_Occurs()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var deptId = Guid.NewGuid();
        var jobId = Guid.NewGuid();
        var skillId = Guid.NewGuid();
        await SeedDataAsync(companyId, userId, deptId, jobId, skillId);

        _tenantService.CompanyId = companyId;

        var command = new UpdateJobCommand(
            jobId,
            "Title",
            "Description",
            "Requirements",
            "Responsibilities",
            deptId,
            userId,
            EmploymentType.FullTime,
            null, null, "Location", null, new List<Guid> { skillId },
            new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 }); // Mismatched RowVersion

        // Act
        var result = await SendAsync(command);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorCodes.ConcurrencyConflict, result.Error);
    }

    [Fact]
    public async Task UpdateJob_Endpoint_Should_Return_Forbidden_When_User_Is_Interviewer()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var deptId = Guid.NewGuid();
        var jobId = Guid.NewGuid();
        await SeedDataAsync(companyId, userId, deptId, jobId, Guid.NewGuid());

        var user = new User { Id = userId, CompanyId = companyId, Email = "interviewer@test.com", FirstName = "Int", LastName = "View", IsActive = true };
        var token = GenerateTokenForUser(user, new[] { "Interviewer" });

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

        var request = new UpdateJobRequest(
            "Title", "Description", "Requirements", "Responsibilities",
            deptId, userId, EmploymentType.FullTime, null, null, "Location", null, new List<Guid>(), rowVersion);

        // Act
        var response = await client.PutAsJsonAsync($"/api/jobs/{jobId}", request);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdateJob_Endpoint_Should_Return_Conflict_When_Concurrency_Error_Occurs()
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

        var request = new UpdateJobRequest(
            "Title", "Description", "Requirements", "Responsibilities",
            deptId, userId, EmploymentType.FullTime, null, null, "Location", null, new List<Guid>(), 
            new byte[] { 9, 9, 9, 9, 9, 9, 9, 9 }); // Concurrency mismatch

        // Act
        var response = await client.PutAsJsonAsync($"/api/jobs/{jobId}", request);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}
