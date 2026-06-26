using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using RecruitIQ.Domain.Entities;

namespace RecruitIQ.Application.Common.Interfaces;

public interface IRecruitIQDbContext
{
    IQueryable<Company> Companies { get; }
    IQueryable<CompanySettings> CompanySettings { get; }
    IQueryable<Department> Departments { get; }
    IQueryable<User> Users { get; }
    IQueryable<Role> Roles { get; }
    IQueryable<UserRole> UserRoles { get; }
    IQueryable<RefreshToken> RefreshTokens { get; }
    IQueryable<Job> Jobs { get; }
    IQueryable<JobVersion> JobVersions { get; }
    IQueryable<Skill> Skills { get; }
    IQueryable<JobSkill> JobSkills { get; }
    IQueryable<Candidate> Candidates { get; }
    IQueryable<Resume> Resumes { get; }
    IQueryable<CandidateExperience> CandidateExperiences { get; }
    IQueryable<CandidateEducation> CandidateEducations { get; }
    IQueryable<CandidateCertification> CandidateCertifications { get; }
    IQueryable<CandidateSkill> CandidateSkills { get; }
    IQueryable<HiringStage> HiringStages { get; }
    IQueryable<RecruitIQ.Domain.Entities.Application> Applications { get; }
    IQueryable<Interview> Interviews { get; }
    IQueryable<InterviewFeedback> InterviewFeedbacks { get; }
    IQueryable<AIAnalysis> AIAnalyses { get; }
    IQueryable<Notification> Notifications { get; }
    IQueryable<Activity> Activities { get; }
    IQueryable<AuditLog> AuditLogs { get; }
    IQueryable<PasswordResetToken> PasswordResetTokens { get; }

    void Add<TEntity>(TEntity entity) where TEntity : class;
    void Update<TEntity>(TEntity entity) where TEntity : class;
    void Remove<TEntity>(TEntity entity) where TEntity : class;

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
