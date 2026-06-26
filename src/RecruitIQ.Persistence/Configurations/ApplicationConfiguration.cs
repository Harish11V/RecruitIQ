using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RecruitIQ.Domain.Entities;
using RecruitIQ.Persistence.Configurations.Base;

namespace RecruitIQ.Persistence.Configurations;

public class ApplicationConfiguration : BaseEntityConfiguration<RecruitIQ.Domain.Entities.Application>
{
    public override void Configure(EntityTypeBuilder<RecruitIQ.Domain.Entities.Application> builder)
    {
        base.Configure(builder);

        builder.HasOne(a => a.Company)
            .WithMany(c => c.Applications)
            .HasForeignKey(a => a.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Job)
            .WithMany(j => j.Applications)
            .HasForeignKey(a => a.JobId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Candidate)
            .WithMany(c => c.Applications)
            .HasForeignKey(a => a.CandidateId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.CurrentStage)
            .WithMany(s => s.Applications)
            .HasForeignKey(a => a.CurrentStageId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => new { a.CompanyId, a.CandidateId, a.JobId, a.CurrentStageId });
        builder.HasIndex(a => a.CompanyId);
    }
}
