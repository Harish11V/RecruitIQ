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
using Microsoft.IdentityModel.Tokens;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using RecruitIQ.Application;
using RecruitIQ.Application.Common.Interfaces;
using RecruitIQ.Application.Features.Jobs.CreateJob;
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

public class TestTenantService : ITenantService
{
    public Guid CompanyId { get; set; }
}

public class TestCurrentUserService : ICurrentUserService
{
    public string? UserId { get; set; } = "TestUser";
}

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection;

    public CustomWebApplicationFactory(SqliteConnection connection)
    {
        _connection = connection;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // Remove standard DbContextOptions
            var dbContextDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<RecruitIQDbContext>));
            if (dbContextDescriptor != null)
            {
                services.Remove(dbContextDescriptor);
            }

            // Register with the shared connection directly
            services.AddScoped<DbContextOptions<RecruitIQDbContext>>(provider =>
            {
                var optionsBuilder = new DbContextOptionsBuilder<RecruitIQDbContext>();
                
                var auditInterceptor = provider.GetRequiredService<AuditEntityInterceptor>();
                var softDeleteInterceptor = provider.GetRequiredService<SoftDeleteInterceptor>();

                optionsBuilder.UseSqlite(_connection)
                              .AddInterceptors(auditInterceptor, softDeleteInterceptor);

                return optionsBuilder.Options;
            });

            // Post-configure JwtBearerOptions to use our test key/issuer/audience
            services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.TokenValidationParameters.IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes("super_secret_key_1234567890123456_recruit_iq_tests"));
                options.TokenValidationParameters.ValidIssuer = "RecruitIQTests";
                options.TokenValidationParameters.ValidAudience = "RecruitIQTests";
            });
        });
    }
}

public class CreateJobTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ServiceProvider _serviceProvider;
    private readonly TestTenantService _tenantService;
    private readonly TestCurrentUserService _currentUserService;

    public CreateJobTests()
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

    private async Task SeedCompanyAndUserAsync(Guid companyId, Guid userId, Guid? departmentId = null, Guid? skillId = null)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<RecruitIQDbContext>();

        var company = new Company
        {
            Id = companyId,
            Name = "Test Company",
            Subdomain = $"test-{companyId:N}",
            IsActive = true
        };
        context.Companies.Add(company);

        var user = new User
        {
            Id = userId,
            CompanyId = companyId,
            Email = $"user-{userId:N}@test.com",
            FirstName = "Test",
            LastName = "User",
            PasswordHash = "hashed",
            IsActive = true
        };
        context.Users.Add(user);

        if (departmentId.HasValue)
        {
            var department = new Department
            {
                Id = departmentId.Value,
                CompanyId = companyId,
                Name = "Engineering",
                Description = "Engineering Dept"
            };
            context.Departments.Add(department);
        }

        if (skillId.HasValue)
        {
            var skill = new Skill
            {
                Id = skillId.Value,
                Name = "C#"
            };
            context.Skills.Add(skill);
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
    public async Task CreateJob_Should_Succeed_With_Valid_Command()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var departmentId = Guid.NewGuid();
        var skillId = Guid.NewGuid();
        await SeedCompanyAndUserAsync(companyId, userId, departmentId, skillId);

        _tenantService.CompanyId = companyId;
        _currentUserService.UserId = userId.ToString();

        var command = new CreateJobCommand(
            "Software Engineer",
            "Job Description",
            "Job Requirements",
            "New York",
            EmploymentType.FullTime,
            50000,
            120000,
            null,
            departmentId,
            DateTime.UtcNow.AddDays(10),
            new List<Guid> { skillId });

        // Act
        var result = await SendAsync(command);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value);

        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<RecruitIQDbContext>();

        var job = context.Jobs.Include(j => j.JobSkills).FirstOrDefault(j => j.Id == result.Value);
        Assert.NotNull(job);
        Assert.Equal(companyId, job.CompanyId);
        Assert.Equal(departmentId, job.DepartmentId);
        Assert.Equal("Software Engineer", job.Title);
        Assert.Equal(EmploymentType.FullTime, job.EmploymentType);
        Assert.Equal(JobStatus.Draft, job.Status);
        Assert.Equal(50000, job.SalaryMin);
        Assert.Equal(120000, job.SalaryMax);
        Assert.Null(job.PublishedAt);
        Assert.NotNull(job.ClosingDate);
        Assert.Single(job.JobSkills);
        Assert.Equal(skillId, job.JobSkills.First().SkillId);

        // Verify sequential job code and slug
        Assert.Equal("JOB-" + DateTime.UtcNow.Year + "-0001", job.JobCode);
        Assert.Equal("software-engineer-0001", job.Slug);

        // Verify Audits
        Assert.Equal(userId.ToString(), job.CreatedBy);
        Assert.True((DateTime.UtcNow - job.CreatedAt).TotalSeconds < 5);

        // Verify Activity Log
        var activity = context.Activities.FirstOrDefault(a => a.EntityId == job.Id);
        Assert.NotNull(activity);
        Assert.Equal($"Job Created: Software Engineer (Engineering) [JOB-{DateTime.UtcNow.Year}-0001]", activity.Action);
        Assert.Equal("Jobs", activity.EntityName);
        Assert.Equal(companyId, activity.CompanyId);
    }

    [Fact]
    public async Task CreateJob_Should_Fail_When_SalaryMax_Less_Than_SalaryMin()
    {
        // Arrange
        var command = new CreateJobCommand(
            "Software Engineer",
            "Description",
            "Requirements",
            "New York",
            EmploymentType.FullTime,
            100000,
            50000, // Invalid: Max < Min
            null,
            Guid.NewGuid(),
            null,
            new List<Guid>());

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => SendAsync(command));
    }

    [Fact]
    public async Task CreateJob_Should_Fail_When_SalaryMin_Is_Too_Large()
    {
        // Arrange
        var command = new CreateJobCommand(
            "Software Engineer",
            "Description",
            "Requirements",
            "New York",
            EmploymentType.FullTime,
            12000000, // Invalid: > 10,000,000
            null,
            null,
            Guid.NewGuid(),
            null,
            new List<Guid>());

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => SendAsync(command));
    }

    [Fact]
    public async Task CreateJob_Should_Fail_When_Department_Does_Not_Exist()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        await SeedCompanyAndUserAsync(companyId, userId);

        _tenantService.CompanyId = companyId;
        _currentUserService.UserId = userId.ToString();

        var command = new CreateJobCommand(
            "Software Engineer",
            "Description",
            "Requirements",
            "New York",
            EmploymentType.FullTime,
            null,
            null,
            null,
            Guid.NewGuid(), // Non-existent department
            null,
            new List<Guid>());

        // Act
        var result = await SendAsync(command);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Department does not exist or does not belong to the current tenant.", result.Error);
    }

    [Fact]
    public async Task CreateJob_Should_Fail_When_RequiredSkills_Contain_Duplicates()
    {
        // Arrange
        var skillId = Guid.NewGuid();
        var command = new CreateJobCommand(
            "Software Engineer",
            "Description",
            "Requirements",
            "New York",
            EmploymentType.FullTime,
            null,
            null,
            null,
            Guid.NewGuid(),
            null,
            new List<Guid> { skillId, skillId }); // Duplicated skill

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => SendAsync(command));
    }

    [Fact]
    public async Task CreateJob_Should_Enforce_Tenant_Isolation()
    {
        // Arrange
        var tenant1 = Guid.NewGuid();
        var user1 = Guid.NewGuid();
        var department1 = Guid.NewGuid();
        await SeedCompanyAndUserAsync(tenant1, user1, department1);

        var tenant2 = Guid.NewGuid();
        var user2 = Guid.NewGuid();
        var department2 = Guid.NewGuid();
        await SeedCompanyAndUserAsync(tenant2, user2, department2);

        // Try creating job in Tenant 1 but referencing Tenant 2's department
        _tenantService.CompanyId = tenant1;
        _currentUserService.UserId = user1.ToString();

        var command = new CreateJobCommand(
            "Software Engineer",
            "Description",
            "Requirements",
            "New York",
            EmploymentType.FullTime,
            null,
            null,
            null,
            department2, // Tenant 2's department
            null,
            new List<Guid>());

        // Act
        var result = await SendAsync(command);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Department does not exist or does not belong to the current tenant.", result.Error);
    }

    [Fact]
    public async Task CreateJob_Endpoint_Should_Return_Forbidden_When_User_Is_Interviewer()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        await SeedCompanyAndUserAsync(companyId, userId);

        var user = new User
        {
            Id = userId,
            CompanyId = companyId,
            Email = $"interviewer@{companyId:N}.com",
            FirstName = "John",
            LastName = "Doe",
            IsActive = true
        };

        var token = GenerateTokenForUser(user, new[] { "Interviewer" });

        using var factory = new CustomWebApplicationFactory(_connection);
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Add("X-Tenant", $"test-{companyId:N}");

        var request = new CreateJobRequest(
            "Software Engineer",
            "Description",
            "Requirements",
            "New York",
            EmploymentType.FullTime,
            50000,
            100000,
            null,
            Guid.NewGuid(),
            null,
            null);

        // Act
        var response = await client.PostAsJsonAsync("/api/jobs", request);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateJob_Endpoint_Should_Return_Ok_When_User_Is_Recruiter()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var departmentId = Guid.NewGuid();
        await SeedCompanyAndUserAsync(companyId, userId, departmentId);

        var user = new User
        {
            Id = userId,
            CompanyId = companyId,
            Email = $"recruiter@{companyId:N}.com",
            FirstName = "John",
            LastName = "Doe",
            IsActive = true
        };

        var token = GenerateTokenForUser(user, new[] { "Recruiter" });

        using var factory = new CustomWebApplicationFactory(_connection);
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Add("X-Tenant", $"test-{companyId:N}");

        var request = new CreateJobRequest(
            "Software Engineer",
            "Description",
            "Requirements",
            "New York",
            EmploymentType.FullTime,
            50000,
            100000,
            null,
            departmentId,
            null,
            null);

        // Act
        var response = await client.PostAsJsonAsync("/api/jobs", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<Guid>>();
        Assert.NotNull(apiResponse);
        Assert.True(apiResponse.Success);
        Assert.NotEqual(Guid.Empty, apiResponse.Data);
    }

    [Fact]
    public async Task CreateJob_Endpoint_Should_Return_BadRequest_When_EmploymentType_Is_Invalid()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var departmentId = Guid.NewGuid();
        await SeedCompanyAndUserAsync(companyId, userId, departmentId);

        var user = new User
        {
            Id = userId,
            CompanyId = companyId,
            Email = $"recruiter@{companyId:N}.com",
            FirstName = "John",
            LastName = "Doe",
            IsActive = true
        };

        var token = GenerateTokenForUser(user, new[] { "Recruiter" });

        using var factory = new CustomWebApplicationFactory(_connection);
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Add("X-Tenant", $"test-{companyId:N}");

        var jsonPayload = $@"{{
            ""title"": ""Software Engineer"",
            ""description"": ""Description"",
            ""requirements"": ""Requirements"",
            ""location"": ""New York"",
            ""employmentType"": ""InvalidType"",
            ""salaryMin"": 50000,
            ""salaryMax"": 100000,
            ""departmentId"": ""{departmentId}""
        }}";
        var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

        // Act
        var response = await client.PostAsync("/api/jobs", content);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
