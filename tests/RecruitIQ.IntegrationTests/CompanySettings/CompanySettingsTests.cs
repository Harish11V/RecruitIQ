using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentValidation;
using MediatR;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RecruitIQ.Application;
using RecruitIQ.Application.Common.Interfaces;
using RecruitIQ.Application.Features.CompanySettings.GetCompanySettings;
using RecruitIQ.Application.Features.CompanySettings.UpdateCompanySettings;
using RecruitIQ.Application.Features.CompanySettings.UploadCompanyLogo;
using RecruitIQ.Common;
using RecruitIQ.Domain.Entities;
using RecruitIQ.Infrastructure;
using RecruitIQ.Infrastructure.Services;
using RecruitIQ.Persistence.DbContext;
using RecruitIQ.Persistence.Interceptors;
using Xunit;

namespace RecruitIQ.IntegrationTests.CompanySettings;

public class TestTenantService : ITenantService
{
    public Guid CompanyId { get; set; }
}

public class TestCurrentUserService : ICurrentUserService
{
    public string? UserId { get; set; } = "TestUser";
}

public class CompanySettingsTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ServiceProvider _serviceProvider;
    private readonly TestTenantService _tenantService;
    private readonly TestCurrentUserService _currentUserService;
    private readonly List<string> _uploadedFilePaths = new();

    public CompanySettingsTests()
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

        services.Configure<FileStorageOptions>(options =>
        {
            options.UploadFolder = "wwwroot/uploads/test_logos"; // Custom test folder
        });
        services.AddScoped<IFileStorageService, LocalFileStorageService>();

        _serviceProvider = services.BuildServiceProvider();

        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<RecruitIQDbContext>();
        context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        var contentRoot = AppContext.BaseDirectory;
        foreach (var path in _uploadedFilePaths)
        {
            var physicalPath = Path.Combine(contentRoot, "wwwroot", path.TrimStart('/'));
            if (File.Exists(physicalPath))
            {
                try { File.Delete(physicalPath); } catch { }
            }
        }
        
        var testFolder = Path.Combine(contentRoot, "wwwroot/uploads/test_logos");
        if (Directory.Exists(testFolder))
        {
            try { Directory.Delete(testFolder, true); } catch { }
        }

        _serviceProvider.Dispose();
        _connection.Dispose();
    }

    private async Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request)
    {
        using var scope = _serviceProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        return await mediator.Send(request);
    }

    private async Task SeedCompanyAndUserAsync(Guid companyId, Guid userId)
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

        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task GetCompanySettings_Should_Return_Defaults_If_None_Exist()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        await SeedCompanyAndUserAsync(companyId, Guid.NewGuid());
        _tenantService.CompanyId = companyId;

        // Act
        var result = await SendAsync(new GetCompanySettingsQuery());

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(companyId, result.Value!.CompanyId);
        Assert.Equal("Light", result.Value.Theme);
        Assert.Equal("UTC", result.Value.Timezone);
        Assert.Equal(30, result.Value.DefaultInterviewDuration);
        Assert.Null(result.Value.LogoUrl);
    }

    [Fact]
    public async Task UpdateCompanySettings_Should_Modify_Settings_And_Log_Activity()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        await SeedCompanyAndUserAsync(companyId, userId);
        _tenantService.CompanyId = companyId;
        _currentUserService.UserId = userId.ToString();

        // Act
        var updateCommand = new UpdateCompanySettingsCommand("Dark", "EST", 45, "test.com", Array.Empty<byte>());
        var updateResult = await SendAsync(updateCommand);
        Assert.True(updateResult.IsSuccess);

        // Fetch settings to verify
        var getResult = await SendAsync(new GetCompanySettingsQuery());
        Assert.True(getResult.IsSuccess);
        Assert.Equal("Dark", getResult.Value!.Theme);
        Assert.Equal("EST", getResult.Value.Timezone);
        Assert.Equal(45, getResult.Value.DefaultInterviewDuration);
        Assert.Equal("test.com", getResult.Value.AllowedEmailDomain);

        // Verify activity log
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<RecruitIQDbContext>();
        var activity = context.Activities.FirstOrDefault(a => a.Action == "Company Settings Updated");
        Assert.NotNull(activity);
        Assert.Equal(companyId, activity.CompanyId);
        Assert.Equal(Guid.Parse(_currentUserService.UserId), activity.UserId);
    }

    [Fact]
    public async Task UploadLogo_Should_Validate_Image_Headers_And_Save_Relative_Path()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        await SeedCompanyAndUserAsync(companyId, userId);
        _tenantService.CompanyId = companyId;
        _currentUserService.UserId = userId.ToString();

        // Valid PNG Image Stream (Magic bytes: 89 50 4E 47 0D 0A 1A 0A)
        var pngBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x00 };
        using var validStream = new MemoryStream(pngBytes);
        var uploadCommand = new UploadCompanyLogoCommand(validStream, "logo.png", "image/png");

        // Act
        var result = await SendAsync(uploadCommand);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.StartsWith("/uploads/test_logos/company-", result.Value);
        Assert.EndsWith(".png", result.Value);

        _uploadedFilePaths.Add(result.Value!);

        var getResult = await SendAsync(new GetCompanySettingsQuery());
        Assert.True(getResult.IsSuccess);
        Assert.Equal(result.Value, getResult.Value!.LogoUrl);
    }

    [Fact]
    public async Task UploadLogo_Should_Reject_Malicious_Non_Image_File()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        await SeedCompanyAndUserAsync(companyId, userId);
        _tenantService.CompanyId = companyId;
        _currentUserService.UserId = userId.ToString();

        var maliciousBytes = Encoding.UTF8.GetBytes("<?php echo 'malicious code'; ?>");
        using var invalidStream = new MemoryStream(maliciousBytes);
        var uploadCommand = new UploadCompanyLogoCommand(invalidStream, "exploit.png", "image/png");

        // Act
        var result = await SendAsync(uploadCommand);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("The uploaded file signature is not a valid image format.", result.Error);
    }

    [Fact]
    public async Task UploadLogo_Should_Reject_Large_File()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        await SeedCompanyAndUserAsync(companyId, userId);
        _tenantService.CompanyId = companyId;
        _currentUserService.UserId = userId.ToString();

        var largeStream = new MemoryStream(new byte[3 * 1024 * 1024]);
        var uploadCommand = new UploadCompanyLogoCommand(largeStream, "logo.png", "image/png");

        // Act
        var result = await SendAsync(uploadCommand);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("File size exceeds the 2 MB limit.", result.Error);
    }

    [Fact]
    public async Task UploadLogo_Should_Delete_Old_Logo_During_Rotation()
    {
        // Arrange
        var companyId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        await SeedCompanyAndUserAsync(companyId, userId);
        _tenantService.CompanyId = companyId;
        _currentUserService.UserId = userId.ToString();

        var pngBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x00 };
        
        using var stream1 = new MemoryStream(pngBytes);
        var upload1 = new UploadCompanyLogoCommand(stream1, "logo1.png", "image/png");
        var res1 = await SendAsync(upload1);
        Assert.True(res1.IsSuccess);
        _uploadedFilePaths.Add(res1.Value!);

        var contentRoot = AppContext.BaseDirectory;
        var physicalPath1 = Path.Combine(contentRoot, "wwwroot", res1.Value!.TrimStart('/'));
        Assert.True(File.Exists(physicalPath1));

        using var stream2 = new MemoryStream(pngBytes);
        var upload2 = new UploadCompanyLogoCommand(stream2, "logo2.png", "image/png");
        var res2 = await SendAsync(upload2);
        Assert.True(res2.IsSuccess);
        _uploadedFilePaths.Add(res2.Value!);

        Assert.False(File.Exists(physicalPath1));
        var physicalPath2 = Path.Combine(contentRoot, "wwwroot", res2.Value!.TrimStart('/'));
        Assert.True(File.Exists(physicalPath2));
    }

    [Fact]
    public async Task TenantIsolation_Should_Prevent_Cross_Tenant_Settings_Access()
    {
        // Arrange
        var tenant1 = Guid.NewGuid();
        var tenant2 = Guid.NewGuid();
        await SeedCompanyAndUserAsync(tenant1, Guid.NewGuid());
        await SeedCompanyAndUserAsync(tenant2, Guid.NewGuid());

        _tenantService.CompanyId = tenant1;
        var updateCommand = new UpdateCompanySettingsCommand("Dark", "EST", 45, "tenant1.com", Array.Empty<byte>());
        var updateResult = await SendAsync(updateCommand);
        Assert.True(updateResult.IsSuccess);

        _tenantService.CompanyId = tenant2;
        var getResult = await SendAsync(new GetCompanySettingsQuery());

        Assert.True(getResult.IsSuccess);
        Assert.Equal(tenant2, getResult.Value!.CompanyId);
        Assert.Equal("Light", getResult.Value.Theme);
        Assert.Null(getResult.Value.AllowedEmailDomain);
    }
}
