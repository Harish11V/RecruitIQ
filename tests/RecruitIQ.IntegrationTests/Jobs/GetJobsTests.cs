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
using RecruitIQ.Application.Features.Jobs.GetJobs;
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

public class GetJobsTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ServiceProvider _serviceProvider;
    private readonly TestTenantService _tenantService;
    private readonly TestCurrentUserService _currentUserService;

    public GetJobsTests()
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
        Guid deptEngId,
        Guid deptOpsId)
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<RecruitIQDbContext>();

        var company = new Company { Id = companyId, Name = "Test Company", Subdomain = $"test-{companyId:N}", IsActive = true };
        context.Companies.Add(company);

        var user = new User { Id = userId, CompanyId = companyId, Email = "test@user.com", FirstName = "John", LastName = "Doe", PasswordHash = "hash", IsActive = true };
        context.Users.Add(user);

        var engDept = new Department { Id = deptEngId, CompanyId = companyId, Name = "Engineering", Description = "Eng" };
        var opsDept = new Department { Id = deptOpsId, CompanyId = companyId, Name = "Operations", Description = "Ops" };
        context.Departments.AddRange(engDept, opsDept);

        // Job 1 (Newest)
        var job1 = new Job
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            DepartmentId = deptEngId,
            Title = "Software Engineer Backend",
            JobCode = "JOB-2026-0001",
            Description = "C# Backend development role",
            Location = "New York",
            EmploymentType = EmploymentType.FullTime,
            Status = JobStatus.Draft,
            HiringManagerId = userId,
            ClosingDate = DateTime.UtcNow.AddDays(10),
            Slug = "software-engineer-backend-0001"
        };
        // Set CreatedAt manually for sorting tests
        job1.CreatedAt = DateTime.UtcNow.AddMinutes(-5);
        job1.CreatedBy = userId.ToString();

        // Job 2 (Older)
        var job2 = new Job
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            DepartmentId = deptEngId,
            Title = "Frontend Developer",
            JobCode = "JOB-2026-0002",
            Description = "Angular Frontend role",
            Location = "New York",
            EmploymentType = EmploymentType.Contract,
            Status = JobStatus.Published,
            HiringManagerId = userId,
            ClosingDate = DateTime.UtcNow.AddDays(5),
            Slug = "frontend-developer-0002"
        };
        job2.CreatedAt = DateTime.UtcNow.AddMinutes(-10);
        job2.CreatedBy = userId.ToString();

        // Job 3 (Oldest)
        var job3 = new Job
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            DepartmentId = deptOpsId,
            Title = "DevOps Engineer",
            JobCode = "JOB-2026-0003",
            Description = "AWS Infrastructure and CI/CD pipelines",
            Location = "Boston",
            EmploymentType = EmploymentType.FullTime,
            Status = JobStatus.Draft,
            HiringManagerId = null,
            ClosingDate = DateTime.UtcNow.AddDays(20),
            Slug = "devops-engineer-0003"
        };
        job3.CreatedAt = DateTime.UtcNow.AddMinutes(-20);
        job3.CreatedBy = userId.ToString();

        context.Jobs.AddRange(job1, job2, job3);
        await context.SaveChangesAsync();

        // Update CreatedAt manually and save again to bypass the interceptor's "Added" state override
        job1.CreatedAt = DateTime.UtcNow.AddMinutes(-5);
        job2.CreatedAt = DateTime.UtcNow.AddMinutes(-10);
        job3.CreatedAt = DateTime.UtcNow.AddMinutes(-20);
        await context.SaveChangesAsync();
    }

    private string GenerateTokenForUser(User user, string[] roles)
    {
        using var scope = _serviceProvider.CreateScope();
        var generator = scope.ServiceProvider.GetRequiredService<IJwtTokenGenerator>();
        return generator.GenerateToken(user, roles);
    }

    [Fact]
    public async Task GetJobs_Should_Return_Paginated_Jobs()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var deptEngId = Guid.NewGuid();
        var deptOpsId = Guid.NewGuid();
        await SeedDataAsync(companyId, userId, deptEngId, deptOpsId);

        _tenantService.CompanyId = companyId;

        // Page 1, Size 2
        var query = new GetJobsQuery(1, 2, null, JobSortOption.CreatedAtDesc, null, null, null, null, null);

        // Act
        var result = await SendAsync(query);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(1, result.Value.Page);
        Assert.Equal(2, result.Value.PageSize);
        Assert.Equal(3, result.Value.TotalRecords);
        Assert.Equal(2, result.Value.TotalPages);
        Assert.False(result.Value.HasPreviousPage);
        Assert.True(result.Value.HasNextPage);
        Assert.Equal(2, result.Value.Items.Count);

        // Page 2, Size 2
        var queryPage2 = new GetJobsQuery(2, 2, null, JobSortOption.CreatedAtDesc, null, null, null, null, null);
        var resultPage2 = await SendAsync(queryPage2);

        Assert.True(resultPage2.IsSuccess);
        Assert.Equal(2, resultPage2.Value.Page);
        Assert.True(resultPage2.Value.HasPreviousPage);
        Assert.False(resultPage2.Value.HasNextPage);
        Assert.Single(resultPage2.Value.Items);
    }

    [Fact]
    public async Task GetJobs_Should_Search_Case_Insensitive()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var deptEngId = Guid.NewGuid();
        var deptOpsId = Guid.NewGuid();
        await SeedDataAsync(companyId, userId, deptEngId, deptOpsId);

        _tenantService.CompanyId = companyId;

        // Search in title/description (dev) - case insensitive
        var query = new GetJobsQuery(1, 10, "dEv", JobSortOption.CreatedAtDesc, null, null, null, null, null);

        // Act
        var result = await SendAsync(query);

        // Assert
        Assert.True(result.IsSuccess);
        // "Software Engineer Backend" matches description "Backend development role"
        // "Frontend Developer" matches Title "Frontend Developer"
        // "DevOps Engineer" matches Title "DevOps Engineer"
        // Total matched: 3
        Assert.Equal(3, result.Value.TotalRecords);

        // Search in department name (Engineering)
        var queryDept = new GetJobsQuery(1, 10, "engineering", JobSortOption.CreatedAtDesc, null, null, null, null, null);
        var resultDept = await SendAsync(queryDept);
        Assert.Equal(2, resultDept.Value.TotalRecords); // Job 1 and Job 2 belong to Engineering
    }

    [Fact]
    public async Task GetJobs_Should_Apply_Optional_Filters()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var deptEngId = Guid.NewGuid();
        var deptOpsId = Guid.NewGuid();
        await SeedDataAsync(companyId, userId, deptEngId, deptOpsId);

        _tenantService.CompanyId = companyId;

        // Status Filter (Published)
        var queryStatus = new GetJobsQuery(1, 10, null, JobSortOption.CreatedAtDesc, JobStatus.Published, null, null, null, null);
        var resultStatus = await SendAsync(queryStatus);
        Assert.Single(resultStatus.Value.Items);
        Assert.Equal("Frontend Developer", resultStatus.Value.Items[0].Title);

        // Department Filter (Operations)
        var queryDept = new GetJobsQuery(1, 10, null, JobSortOption.CreatedAtDesc, null, deptOpsId, null, null, null);
        var resultDept = await SendAsync(queryDept);
        Assert.Single(resultDept.Value.Items);
        Assert.Equal("DevOps Engineer", resultDept.Value.Items[0].Title);

        // Employment Type Filter (Contract)
        var queryType = new GetJobsQuery(1, 10, null, JobSortOption.CreatedAtDesc, null, null, EmploymentType.Contract, null, null);
        var resultType = await SendAsync(queryType);
        Assert.Single(resultType.Value.Items);

        // Location Filter (New York)
        var queryLoc = new GetJobsQuery(1, 10, null, JobSortOption.CreatedAtDesc, null, null, null, null, "NEW YORK");
        var resultLoc = await SendAsync(queryLoc);
        Assert.Equal(2, resultLoc.Value.TotalRecords);
    }

    [Fact]
    public async Task GetJobs_Should_Sort_Correctly()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var deptEngId = Guid.NewGuid();
        var deptOpsId = Guid.NewGuid();
        await SeedDataAsync(companyId, userId, deptEngId, deptOpsId);

        _tenantService.CompanyId = companyId;

        // CreatedAt Ascending (Oldest first: DevOps -> Frontend -> Backend)
        var queryAsc = new GetJobsQuery(1, 10, null, JobSortOption.CreatedAtAsc, null, null, null, null, null);
        var resultAsc = await SendAsync(queryAsc);
        Assert.Equal("DevOps Engineer", resultAsc.Value.Items[0].Title);
        Assert.Equal("Frontend Developer", resultAsc.Value.Items[1].Title);
        Assert.Equal("Software Engineer Backend", resultAsc.Value.Items[2].Title);

        // Title Ascending (DevOps -> Frontend -> Software)
        var queryTitle = new GetJobsQuery(1, 10, null, JobSortOption.TitleAsc, null, null, null, null, null);
        var resultTitle = await SendAsync(queryTitle);
        Assert.Equal("DevOps Engineer", resultTitle.Value.Items[0].Title);
        Assert.Equal("Frontend Developer", resultTitle.Value.Items[1].Title);
        Assert.Equal("Software Engineer Backend", resultTitle.Value.Items[2].Title);
    }

    [Fact]
    public async Task GetJobs_Should_Enforce_Tenant_Isolation()
    {
        // Arrange
        var company1 = Guid.NewGuid();
        var user1 = Guid.NewGuid();
        var dept1 = Guid.NewGuid();
        await SeedDataAsync(company1, user1, dept1, Guid.NewGuid());

        var company2 = Guid.NewGuid();
        var user2 = Guid.NewGuid();
        var dept2 = Guid.NewGuid();
        await SeedDataAsync(company2, user2, dept2, Guid.NewGuid());

        // Query for company1 context
        _tenantService.CompanyId = company1;
        var query = new GetJobsQuery(1, 10, null, JobSortOption.CreatedAtDesc, null, null, null, null, null);

        // Act
        var result = await SendAsync(query);

        // Assert
        Assert.True(result.IsSuccess);
        // Should only return the 3 jobs created under company1
        Assert.Equal(3, result.Value.TotalRecords);
        Assert.True(result.Value.Items.All(i => i.DepartmentName == "Engineering" || i.DepartmentName == "Operations"));
    }

    [Fact]
    public async Task GetJobs_Should_Return_Empty_When_No_Jobs_Match()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        await SeedDataAsync(companyId, userId, Guid.NewGuid(), Guid.NewGuid());

        _tenantService.CompanyId = companyId;

        var query = new GetJobsQuery(1, 10, "NonExistentJobTitlePattern", JobSortOption.CreatedAtDesc, null, null, null, null, null);

        // Act
        var result = await SendAsync(query);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value.Items);
        Assert.Equal(0, result.Value.TotalRecords);
        Assert.Equal(0, result.Value.TotalPages);
    }

    [Fact]
    public async Task GetJobs_Endpoint_Should_Return_Unauthorized_When_No_Token_Provided()
    {
        // Arrange
        using var factory = new CustomWebApplicationFactory(_connection);
        var client = factory.CreateClient();

        // Act
        var response = await client.GetAsync("/api/jobs");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetJobs_Endpoint_Should_Return_Forbidden_When_User_Is_Interviewer()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        await SeedDataAsync(companyId, userId, Guid.NewGuid(), Guid.NewGuid());

        var user = new User { Id = userId, CompanyId = companyId, Email = "interviewer@test.com", FirstName = "Int", LastName = "View", IsActive = true };
        var token = GenerateTokenForUser(user, new[] { "Interviewer" });

        using var factory = new CustomWebApplicationFactory(_connection);
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Add("X-Tenant", $"test-{companyId:N}");

        // Act
        var response = await client.GetAsync("/api/jobs");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetJobs_Endpoint_Should_Return_Ok_When_User_Is_Recruiter()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        await SeedDataAsync(companyId, userId, Guid.NewGuid(), Guid.NewGuid());

        var user = new User { Id = userId, CompanyId = companyId, Email = "recruiter@test.com", FirstName = "Rec", LastName = "Ruit", IsActive = true };
        var token = GenerateTokenForUser(user, new[] { "Recruiter" });

        using var factory = new CustomWebApplicationFactory(_connection);
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.Add("X-Tenant", $"test-{companyId:N}");

        // Act
        var response = await client.GetAsync("/api/jobs?page=1&pageSize=2");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var apiResponse = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResponse<JobSummaryResponse>>>();
        Assert.NotNull(apiResponse);
        Assert.True(apiResponse.Success);
        Assert.Equal(2, apiResponse.Data.Items.Count);
        Assert.Equal(3, apiResponse.Data.TotalRecords);
        Assert.Equal(2, apiResponse.Data.TotalPages);
        Assert.False(apiResponse.Data.HasPreviousPage);
        Assert.True(apiResponse.Data.HasNextPage);
        Assert.Equal(0, apiResponse.Data.Items[0].ApplicantCount); // Verify ApplicantCount defaults to 0
    }
}
