using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentValidation;
using MediatR;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RecruitIQ.Application;
using RecruitIQ.Application.Common.Interfaces;
using RecruitIQ.Application.Features.Authentication.CreateCompany;
using RecruitIQ.Application.Features.Authentication.ForgotPassword;
using RecruitIQ.Application.Features.Authentication.InviteUser;
using RecruitIQ.Application.Features.Authentication.Login;
using RecruitIQ.Application.Features.Authentication.Logout;
using RecruitIQ.Application.Features.Authentication.RefreshToken;
using RecruitIQ.Application.Features.Authentication.ResetPassword;
using RecruitIQ.Common;
using RecruitIQ.Domain.Entities;
using RecruitIQ.Infrastructure;
using RecruitIQ.Infrastructure.Services;
using RecruitIQ.Persistence.DbContext;
using RecruitIQ.Persistence.Interceptors;
using Xunit;

namespace RecruitIQ.IntegrationTests.Authentication;

public class TestTenantService : ITenantService
{
    public Guid CompanyId { get; set; }
}

public class TestCurrentUserService : ICurrentUserService
{
    public string? UserId { get; set; } = "TestUser";
}

public class AuthTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ServiceProvider _serviceProvider;
    private readonly TestTenantService _tenantService;
    private readonly TestCurrentUserService _currentUserService;

    public AuthTests()
    {
        // 1. Setup sqlite connection
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();

        // 2. Setup DI
        var services = new ServiceCollection();
        services.AddLogging();

        _tenantService = new TestTenantService();
        _currentUserService = new TestCurrentUserService();

        services.AddSingleton<ITenantService>(_tenantService);
        services.AddSingleton<ICurrentUserService>(_currentUserService);

        // Audit & SoftDelete interceptors
        services.AddScoped<AuditEntityInterceptor>();
        services.AddScoped<SoftDeleteInterceptor>();

        // DbContext
        services.AddDbContext<RecruitIQDbContext>((sp, options) =>
        {
            var auditInterceptor = sp.GetRequiredService<AuditEntityInterceptor>();
            var softDeleteInterceptor = sp.GetRequiredService<SoftDeleteInterceptor>();

            options.UseSqlite(_connection)
                   .AddInterceptors(auditInterceptor, softDeleteInterceptor);
        });

        services.AddScoped<IRecruitIQDbContext>(provider => provider.GetRequiredService<RecruitIQDbContext>());

        // Application services
        services.AddApplicationServices();

        // Infrastructure options
        services.Configure<JwtSettings>(options =>
        {
            options.Secret = "super_secret_key_1234567890123456_recruit_iq_tests";
            options.Issuer = "RecruitIQTests";
            options.Audience = "RecruitIQTests";
            options.ExpiryInMinutes = 60;
        });

        // Hashing & Tokens
        services.AddScoped<IPasswordHasher, BCryptPasswordHasher>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();

        _serviceProvider = services.BuildServiceProvider();

        // Ensure database schema is created
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

    [Fact]
    public async Task Validation_Should_Reject_Simple_Password()
    {
        // Arrange
        var command = new CreateCompanyCommand(
            "admin@test.com",
            "simple", // Invalid password
            "John",
            "Doe",
            "Test Corp",
            "testcorp");

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => SendAsync(command));
    }

    [Fact]
    public async Task EnumerationDefense_Should_Return_Generic_Error_For_NonExistent_Email()
    {
        // Arrange
        _tenantService.CompanyId = Guid.NewGuid(); // Some company
        var command = new LoginCommand("nonexistent@test.com", "Password123!", "127.0.0.1", "UserAgent");

        // Act
        var result = await SendAsync(command);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Invalid email or password.", result.Error);
    }

    [Fact]
    public async Task AccountLockout_Should_Lock_Account_After_5_Failed_Logins()
    {
        // Arrange
        var registerCommand = new CreateCompanyCommand(
            "admin@lockout.com",
            "Password123!",
            "John",
            "Doe",
            "Lockout Corp",
            "lockout");
        var regResult = await SendAsync(registerCommand);
        Assert.True(regResult.IsSuccess);
        var companyId = regResult.Value;

        // Set the active tenant
        _tenantService.CompanyId = companyId;

        // Perform 5 failed logins
        var loginCommand = new LoginCommand("admin@lockout.com", "WrongPassword123!", "127.0.0.1", "UserAgent");
        for (int i = 0; i < 5; i++)
        {
            var res = await SendAsync(loginCommand);
            Assert.False(res.IsSuccess);
            Assert.Equal("Invalid email or password.", res.Error);
        }

        // The 6th login should return account lockout message
        var lockedRes = await SendAsync(loginCommand);
        Assert.False(lockedRes.IsSuccess);
        Assert.Contains("locked", lockedRes.Error);
    }

    [Fact]
    public async Task TenantIsolation_Should_Fail_Login_For_CrossTenant_Credentials()
    {
        // Arrange
        // Tenant A
        var registerA = new CreateCompanyCommand("admin@a.com", "Password123!", "John", "Doe", "Corp A", "corpa");
        var regARes = await SendAsync(registerA);
        Assert.True(regARes.IsSuccess);

        // Tenant B
        var registerB = new CreateCompanyCommand("admin@b.com", "Password123!", "John", "Doe", "Corp B", "corpb");
        var regBRes = await SendAsync(registerB);
        Assert.True(regBRes.IsSuccess);
        var companyBId = regBRes.Value;

        // Act: Attempt to login to Tenant B with Tenant A's email
        _tenantService.CompanyId = companyBId;
        var loginCommand = new LoginCommand("admin@a.com", "Password123!", "127.0.0.1", "UserAgent");
        var result = await SendAsync(loginCommand);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Invalid email or password.", result.Error);
    }

    [Fact]
    public async Task Constraints_Should_Prevent_Duplicate_Subdomain()
    {
        // Arrange
        var registerA = new CreateCompanyCommand("admin@a.com", "Password123!", "John", "Doe", "Corp A", "dupe");
        var resultA = await SendAsync(registerA);
        Assert.True(resultA.IsSuccess);

        var registerB = new CreateCompanyCommand("admin@b.com", "Password123!", "John", "Doe", "Corp B", "dupe");
        
        // Act
        var resultB = await SendAsync(registerB);

        // Assert
        Assert.False(resultB.IsSuccess);
        Assert.Equal("Subdomain is already taken.", resultB.Error);
    }

    [Fact]
    public async Task RefreshTokenRotation_Should_Rotate_Tokens_And_Reject_Revoked_Tokens()
    {
        // Arrange
        var register = new CreateCompanyCommand("admin@refresh.com", "Password123!", "John", "Doe", "Refresh Corp", "refresh");
        var regResult = await SendAsync(register);
        Assert.True(regResult.IsSuccess);
        var companyId = regResult.Value;

        _tenantService.CompanyId = companyId;

        var login = new LoginCommand("admin@refresh.com", "Password123!", "127.0.0.1", "UserAgent");
        var loginResult = await SendAsync(login);
        Assert.True(loginResult.IsSuccess);

        var accessToken = loginResult.Value!.AccessToken;
        var refreshToken = loginResult.Value!.RefreshToken;

        // Act 1: Rotate tokens
        var rotateCommand = new RefreshTokenCommand(accessToken, refreshToken, "127.0.0.1");
        var rotateResult = await SendAsync(rotateCommand);
        Assert.True(rotateResult.IsSuccess);
        Assert.NotNull(rotateResult.Value!.AccessToken);
        Assert.NotNull(rotateResult.Value!.RefreshToken);

        // Act 2: Attempt to rotate again with the old (now revoked) token
        var secondRotateResult = await SendAsync(rotateCommand);

        // Assert
        Assert.False(secondRotateResult.IsSuccess);
        Assert.Equal("Refresh token has been revoked.", secondRotateResult.Error);
    }

    [Fact]
    public async Task RefreshToken_Should_Fail_If_Expired()
    {
        // Arrange
        var register = new CreateCompanyCommand("admin@expired.com", "Password123!", "John", "Doe", "Expired Corp", "expired");
        var regResult = await SendAsync(register);
        Assert.True(regResult.IsSuccess);
        var companyId = regResult.Value;

        _tenantService.CompanyId = companyId;

        var login = new LoginCommand("admin@expired.com", "Password123!", "127.0.0.1", "UserAgent");
        var loginResult = await SendAsync(login);
        Assert.True(loginResult.IsSuccess);

        var accessToken = loginResult.Value!.AccessToken;
        var refreshTokenStr = loginResult.Value!.RefreshToken;

        // Fetch token from db and expire it
        using (var scope = _serviceProvider.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<RecruitIQDbContext>();
            var tokenEntity = context.RefreshTokens.FirstOrDefault(rt => rt.Token == refreshTokenStr);
            Assert.NotNull(tokenEntity);
            tokenEntity.ExpiresAt = DateTime.UtcNow.AddMinutes(-5);
            await context.SaveChangesAsync();
        }

        // Act
        var rotateCommand = new RefreshTokenCommand(accessToken, refreshTokenStr, "127.0.0.1");
        var rotateResult = await SendAsync(rotateCommand);

        // Assert
        Assert.False(rotateResult.IsSuccess);
        Assert.Equal("Refresh token has expired.", rotateResult.Error);
    }
}
