using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using RecruitIQ.Application.Common.Interfaces;
using RecruitIQ.Domain.Entities;
using RecruitIQ.Persistence.DbContext;
using RecruitIQ.Persistence.Interceptors;
using Xunit;

namespace RecruitIQ.IntegrationTests.Persistence;

public class TestTenantService : ITenantService
{
    public Guid CompanyId { get; set; } = Guid.NewGuid();
}

public class TestCurrentUserService : ICurrentUserService
{
    public string? UserId { get; set; } = "TestUser";
}

public class PersistenceTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<RecruitIQDbContext> _options;
    private readonly TestTenantService _tenantService;
    private readonly TestCurrentUserService _currentUserService;

    public PersistenceTests()
    {
        // 1. Create a persistent SQLite connection
        _connection = new SqliteConnection("Filename=:memory:");
        _connection.Open();

        // 2. Set up services
        _tenantService = new TestTenantService();
        _currentUserService = new TestCurrentUserService();

        // 3. Set up DbContextOptions including interceptors
        var auditInterceptor = new AuditEntityInterceptor(_currentUserService);
        var softDeleteInterceptor = new SoftDeleteInterceptor(_currentUserService);

        _options = new DbContextOptionsBuilder<RecruitIQDbContext>()
            .UseSqlite(_connection)
            .AddInterceptors(auditInterceptor, softDeleteInterceptor)
            .Options;

        // 4. Create database schema
        using var context = new RecruitIQDbContext(_options, _tenantService);
        context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    [Fact]
    public async Task DbContext_Should_Initialize_And_Connect()
    {
        // Arrange & Act
        using var context = new RecruitIQDbContext(_options, _tenantService);
        var canConnect = await context.Database.CanConnectAsync();

        // Assert
        Assert.True(canConnect);
    }

    [Fact]
    public async Task AuditingInterceptor_Should_Set_CreatedAt_And_CreatedBy_On_Insert()
    {
        // Arrange
        using var context = new RecruitIQDbContext(_options, _tenantService);
        var company = new Company
        {
            Name = "Auditing Tenant",
            Subdomain = "audit"
        };

        // Act
        context.Companies.Add(company);
        await context.SaveChangesAsync();

        // Assert
        Assert.NotEqual(Guid.Empty, company.Id);
        Assert.Equal("TestUser", company.CreatedBy);
        Assert.True((DateTime.UtcNow - company.CreatedAt).TotalSeconds < 5);
    }

    [Fact]
    public async Task AuditingInterceptor_Should_Set_UpdatedAt_And_UpdatedBy_On_Update()
    {
        // Arrange
        using var context = new RecruitIQDbContext(_options, _tenantService);
        var company = new Company
        {
            Name = "Auditing Tenant Update",
            Subdomain = "audit-update"
        };
        context.Companies.Add(company);
        await context.SaveChangesAsync();

        // Act
        company.Name = "Updated Name";
        _currentUserService.UserId = "UpdaterUser";
        await context.SaveChangesAsync();

        // Assert
        Assert.Equal("UpdaterUser", company.UpdatedBy);
        Assert.NotNull(company.UpdatedAt);
        Assert.True((DateTime.UtcNow - company.UpdatedAt.Value).TotalSeconds < 5);
    }

    [Fact]
    public async Task SoftDeleteInterceptor_Should_Mark_IsDeleted_True_Instead_Of_Hard_Delete()
    {
        // Arrange
        using var context = new RecruitIQDbContext(_options, _tenantService);
        var company = new Company
        {
            Name = "Soft Delete Tenant",
            Subdomain = "soft-delete"
        };
        context.Companies.Add(company);
        await context.SaveChangesAsync();

        // Act
        context.Companies.Remove(company);
        _currentUserService.UserId = "DeleterUser";
        await context.SaveChangesAsync();

        // Assert
        // Re-fetch using IgnoreQueryFilters since the global query filter will hide it by default
        var deletedCompany = await context.Companies
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(c => c.Id == company.Id);

        Assert.NotNull(deletedCompany);
        Assert.True(deletedCompany.IsDeleted);
        Assert.NotNull(deletedCompany.DeletedAt);
        Assert.Equal("DeleterUser", deletedCompany.DeletedBy);
    }

    [Fact]
    public async Task GlobalQueryFilter_Should_Filter_Soft_Deleted_Entities()
    {
        // Arrange
        using var context = new RecruitIQDbContext(_options, _tenantService);
        var company1 = new Company { Name = "Active", Subdomain = "active" };
        var company2 = new Company { Name = "Deleted", Subdomain = "deleted" };
        context.Companies.AddRange(company1, company2);
        await context.SaveChangesAsync();

        context.Companies.Remove(company2);
        await context.SaveChangesAsync();

        // Act
        var activeCompanies = await context.Companies.ToListAsync();

        // Assert
        Assert.Contains(activeCompanies, c => c.Id == company1.Id);
        Assert.DoesNotContain(activeCompanies, c => c.Id == company2.Id);
    }

    [Fact]
    public async Task GlobalQueryFilter_Should_Isolate_Tenant_Data_Only_For_MultiTenant_Entities()
    {
        // Arrange
        var tenant1 = Guid.NewGuid();
        var tenant2 = Guid.NewGuid();

        // Set tenant context to tenant1 to insert data
        _tenantService.CompanyId = tenant1;

        using (var context = new RecruitIQDbContext(_options, _tenantService))
        {
            var company1 = new Company { Id = tenant1, Name = "Tenant 1", Subdomain = "tenant1" };
            context.Companies.Add(company1);

            var dept1 = new Department { Name = "Engineering", CompanyId = tenant1 };
            context.Departments.Add(dept1);
            
            // Seed a global lookup Role (should NOT have tenant filtering applied)
            var role = new Role { Name = "Global Test Role" };
            context.Roles.Add(role);

            await context.SaveChangesAsync();
        }

        // Set tenant context to tenant2 to insert another department
        _tenantService.CompanyId = tenant2;
        using (var context = new RecruitIQDbContext(_options, _tenantService))
        {
            var company2 = new Company { Id = tenant2, Name = "Tenant 2", Subdomain = "tenant2" };
            context.Companies.Add(company2);

            var dept2 = new Department { Name = "Sales", CompanyId = tenant2 };
            context.Departments.Add(dept2);
            await context.SaveChangesAsync();
        }

        // Act & Assert: Query under tenant1
        _tenantService.CompanyId = tenant1;
        using (var context = new RecruitIQDbContext(_options, _tenantService))
        {
            var depts = await context.Departments.ToListAsync();
            Assert.Single(depts);
            Assert.Equal("Engineering", depts[0].Name);

            // Global role should still be queryable under any tenant because it is not tenant filtered
            var roles = await context.Roles.ToListAsync();
            Assert.Contains(roles, r => r.Name == "Global Test Role");
        }

        // Act & Assert: Query under tenant2
        _tenantService.CompanyId = tenant2;
        using (var context = new RecruitIQDbContext(_options, _tenantService))
        {
            var depts = await context.Departments.ToListAsync();
            Assert.Single(depts);
            Assert.Equal("Sales", depts[0].Name);
        }
    }
}
