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
using RecruitIQ.Application.Features.Jobs.GetJobById;
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

public class GetJobByIdTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ServiceProvider _serviceProvider;
    private readonly TestTenantService _tenantService;
    private readonly TestCurrentUserService _currentUserService;

    public GetJobByIdTests()
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
        Guid skillId)
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
            Status = JobStatus.Draft,
            HiringManagerId = userId,
            ClosingDate = DateTime.UtcNow.AddDays(10),
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
    public async Task GetJobById_Should_Return_Detailed_Job_When_Job_Exists()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var deptId = Guid.NewGuid();
        var jobId = Guid.NewGuid();
        var skillId = Guid.NewGuid();
        await SeedDataAsync(companyId, userId, deptId, jobId, skillId);

        _tenantService.CompanyId = companyId;

        var query = new GetJobByIdQuery(jobId);

        // Act
        var result = await SendAsync(query);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(jobId, result.Value.JobId);
        Assert.Equal("JOB-2026-0001", result.Value.JobCode);
        Assert.Equal("Software Engineer Backend", result.Value.Title);
        Assert.Equal("software-engineer-backend-0001", result.Value.Slug);
        Assert.Equal("Description", result.Value.Description);
        Assert.Equal("Requirements", result.Value.Requirements);
        Assert.Equal(string.Empty, result.Value.Responsibilities);
        Assert.Equal(deptId, result.Value.Department.Id);
        Assert.Equal("Engineering", result.Value.Department.Name);
        Assert.NotNull(result.Value.HiringManager);
        Assert.Equal(userId, result.Value.HiringManager.Id);
        Assert.Equal("John Doe", result.Value.HiringManager.FullName);
        Assert.Equal("test@user.com", result.Value.HiringManager.Email);
        Assert.Equal(EmploymentType.FullTime, result.Value.EmploymentType);
        Assert.Equal(JobStatus.Draft, result.Value.Status);
        Assert.Single(result.Value.RequiredSkills);
        Assert.Equal(skillId, result.Value.RequiredSkills[0].Id);
        Assert.Equal($"Skill-{skillId:N}", result.Value.RequiredSkills[0].Name);
        Assert.Equal(0, result.Value.ApplicantCount);
        Assert.Equal(0, result.Value.InterviewCount);
    }

    [Fact]
    public async Task GetJobById_Should_Return_Failure_When_Job_Does_Not_Exist()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        _tenantService.CompanyId = companyId;

        var query = new GetJobByIdQuery(Guid.NewGuid());

        // Act
        var result = await SendAsync(query);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Job not found.", result.Error);
    }

    [Fact]
    public async Task GetJobById_Should_Enforce_Tenant_Isolation()
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

        // Query job2 using company1 tenant context
        _tenantService.CompanyId = company1;
        var query = new GetJobByIdQuery(job2);

        // Act
        var result = await SendAsync(query);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Job not found.", result.Error);
    }

    [Fact]
    public async Task GetJobById_Endpoint_Should_Return_Unauthorized_When_No_Token_Provided()
    {
        // Arrange
        using var factory = new CustomWebApplicationFactory(_connection);
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync($"/api/jobs/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetJobById_Endpoint_Should_Return_Forbidden_When_User_Is_Interviewer()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var deptId = Guid.NewGuid();
        var jobId = Guid.NewGuid();
        await SeedDataAsync(companyId, userId, deptId, jobId, Guid.NewGuid());

        var user = new User { Id = userId, CompanyId = companyId, Email = "interviewer@test.com", FirstName = "Int", LastName = "View", IsActive = true };
        var token = GenerateTokenForUser(user, new[] { "Interviewer" });

        using var factory = new CustomWebApplicationFactory(_connection);
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Add("X-Tenant", $"test-{companyId:N}");

        // Act
        var response = await client.GetAsync($"/api/jobs/{jobId}");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetJobById_Endpoint_Should_Return_NotFound_When_Job_Does_Not_Exist()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        await SeedDataAsync(companyId, userId, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        var user = new User { Id = userId, CompanyId = companyId, Email = "recruiter@test.com", FirstName = "Rec", LastName = "Ruit", IsActive = true };
        var token = GenerateTokenForUser(user, new[] { "Recruiter" });

        using var factory = new CustomWebApplicationFactory(_connection);
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Add("X-Tenant", $"test-{companyId:N}");

        // Act
        var response = await client.GetAsync($"/api/jobs/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetJobById_Endpoint_Should_Return_Ok_When_User_Is_Recruiter()
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

        using var factory = new CustomWebApplicationFactory(_connection);
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Add("X-Tenant", $"test-{companyId:N}");

        // Act
        var response = await client.GetAsync($"/api/jobs/{jobId}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<JobDetailsResponse>>();
        Assert.NotNull(apiResponse);
        Assert.True(apiResponse.Success);
        Assert.Equal(jobId, apiResponse.Data.JobId);
        Assert.Equal("Engineering", apiResponse.Data.Department.Name);
        Assert.Equal("John Doe", apiResponse.Data.HiringManager.FullName);
        Assert.Equal($"Skill-{skillId:N}", apiResponse.Data.RequiredSkills[0].Name);
    }
}
