using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RecruitIQ.Application.Common.Interfaces;
using RecruitIQ.Domain.Entities;
using RecruitIQ.Persistence.Extensions;

namespace RecruitIQ.Persistence.DbContext;

public class RecruitIQDbContext : Microsoft.EntityFrameworkCore.DbContext, IRecruitIQDbContext
{
    private readonly ITenantService _tenantService;

    public RecruitIQDbContext(
        DbContextOptions<RecruitIQDbContext> options,
        ITenantService tenantService) : base(options)
    {
        _tenantService = tenantService;
    }

    public Guid CurrentCompanyId => _tenantService.CompanyId;

    public DbSet<Company> Companies => Set<Company>();
    public DbSet<CompanySettings> CompanySettings => Set<CompanySettings>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<Job> Jobs => Set<Job>();
    public DbSet<JobVersion> JobVersions => Set<JobVersion>();
    public DbSet<Skill> Skills => Set<Skill>();
    public DbSet<JobSkill> JobSkills => Set<JobSkill>();
    public DbSet<Candidate> Candidates => Set<Candidate>();
    public DbSet<Resume> Resumes => Set<Resume>();
    public DbSet<CandidateExperience> CandidateExperiences => Set<CandidateExperience>();
    public DbSet<CandidateEducation> CandidateEducations => Set<CandidateEducation>();
    public DbSet<CandidateCertification> CandidateCertifications => Set<CandidateCertification>();
    public DbSet<CandidateSkill> CandidateSkills => Set<CandidateSkill>();
    public DbSet<HiringStage> HiringStages => Set<HiringStage>();
    public DbSet<RecruitIQ.Domain.Entities.Application> Applications => Set<RecruitIQ.Domain.Entities.Application>();
    public DbSet<Interview> Interviews => Set<Interview>();
    public DbSet<InterviewFeedback> InterviewFeedbacks => Set<InterviewFeedback>();
    public DbSet<AIAnalysis> AIAnalyses => Set<AIAnalysis>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Activity> Activities => Set<Activity>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();

    // Explicit IQueryable implementation mapping to DbSet
    IQueryable<Company> IRecruitIQDbContext.Companies => Companies.AsQueryable();
    IQueryable<CompanySettings> IRecruitIQDbContext.CompanySettings => CompanySettings.AsQueryable();
    IQueryable<Department> IRecruitIQDbContext.Departments => Departments.AsQueryable();
    IQueryable<User> IRecruitIQDbContext.Users => Users.AsQueryable();
    IQueryable<Role> IRecruitIQDbContext.Roles => Roles.AsQueryable();
    IQueryable<UserRole> IRecruitIQDbContext.UserRoles => UserRoles.AsQueryable();
    IQueryable<RefreshToken> IRecruitIQDbContext.RefreshTokens => RefreshTokens.AsQueryable();
    IQueryable<Job> IRecruitIQDbContext.Jobs => Jobs.AsQueryable();
    IQueryable<JobVersion> IRecruitIQDbContext.JobVersions => JobVersions.AsQueryable();
    IQueryable<Skill> IRecruitIQDbContext.Skills => Skills.AsQueryable();
    IQueryable<JobSkill> IRecruitIQDbContext.JobSkills => JobSkills.AsQueryable();
    IQueryable<Candidate> IRecruitIQDbContext.Candidates => Candidates.AsQueryable();
    IQueryable<Resume> IRecruitIQDbContext.Resumes => Resumes.AsQueryable();
    IQueryable<CandidateExperience> IRecruitIQDbContext.CandidateExperiences => CandidateExperiences.AsQueryable();
    IQueryable<CandidateEducation> IRecruitIQDbContext.CandidateEducations => CandidateEducations.AsQueryable();
    IQueryable<CandidateCertification> IRecruitIQDbContext.CandidateCertifications => CandidateCertifications.AsQueryable();
    IQueryable<CandidateSkill> IRecruitIQDbContext.CandidateSkills => CandidateSkills.AsQueryable();
    IQueryable<HiringStage> IRecruitIQDbContext.HiringStages => HiringStages.AsQueryable();
    IQueryable<RecruitIQ.Domain.Entities.Application> IRecruitIQDbContext.Applications => Applications.AsQueryable();
    IQueryable<Interview> IRecruitIQDbContext.Interviews => Interviews.AsQueryable();
    IQueryable<InterviewFeedback> IRecruitIQDbContext.InterviewFeedbacks => InterviewFeedbacks.AsQueryable();
    IQueryable<AIAnalysis> IRecruitIQDbContext.AIAnalyses => AIAnalyses.AsQueryable();
    IQueryable<Notification> IRecruitIQDbContext.Notifications => Notifications.AsQueryable();
    IQueryable<Activity> IRecruitIQDbContext.Activities => Activities.AsQueryable();
    IQueryable<AuditLog> IRecruitIQDbContext.AuditLogs => AuditLogs.AsQueryable();
    IQueryable<PasswordResetToken> IRecruitIQDbContext.PasswordResetTokens => PasswordResetTokens.AsQueryable();

    public new void Add<TEntity>(TEntity entity) where TEntity : class
    {
        base.Add(entity);
    }

    public new void Update<TEntity>(TEntity entity) where TEntity : class
    {
        base.Update(entity);
    }

    public new void Remove<TEntity>(TEntity entity) where TEntity : class
    {
        base.Remove(entity);
    }

    public IQueryable<TEntity> QueryReadOnly<TEntity>() where TEntity : class
    {
        return Set<TEntity>().AsNoTracking();
    }

    public void SetOriginalRowVersion<TEntity>(TEntity entity, byte[] rowVersion) where TEntity : RecruitIQ.Domain.Base.BaseEntity
    {
        Entry(entity).Property(e => e.RowVersion).OriginalValue = rowVersion;
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RecruitIQDbContext).Assembly);
        modelBuilder.ConfigureBaseProperties(this);
        modelBuilder.ApplyGlobalQueryFilters(this);
    }
}
