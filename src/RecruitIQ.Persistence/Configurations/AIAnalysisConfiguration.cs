using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RecruitIQ.Domain.Entities;
using RecruitIQ.Persistence.Configurations.Base;

namespace RecruitIQ.Persistence.Configurations;

public class AIAnalysisConfiguration : BaseEntityConfiguration<AIAnalysis>
{
    public override void Configure(EntityTypeBuilder<AIAnalysis> builder)
    {
        base.Configure(builder);

        builder.Property(aia => aia.Strengths)
            .IsRequired();

        builder.Property(aia => aia.Weaknesses)
            .IsRequired();

        builder.Property(aia => aia.Recommendations)
            .IsRequired();

        builder.HasOne(aia => aia.Company)
            .WithMany()
            .HasForeignKey(aia => aia.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(aia => aia.Resume)
            .WithMany(r => r.AIAnalyses)
            .HasForeignKey(aia => aia.ResumeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(aia => aia.Job)
            .WithMany(j => j.AIAnalyses)
            .HasForeignKey(aia => aia.JobId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(aia => aia.CompanyId);
    }
}
