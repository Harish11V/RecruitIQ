using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RecruitIQ.Domain.Entities;
using RecruitIQ.Persistence.Configurations.Base;

namespace RecruitIQ.Persistence.Configurations;

public class CandidateSkillConfiguration : BaseEntityConfiguration<CandidateSkill>
{
    public override void Configure(EntityTypeBuilder<CandidateSkill> builder)
    {
        base.Configure(builder);

        builder.Property(cs => cs.ProficiencyLevel)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.HasOne(cs => cs.Company)
            .WithMany()
            .HasForeignKey(cs => cs.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(cs => cs.Candidate)
            .WithMany(c => c.CandidateSkills)
            .HasForeignKey(cs => cs.CandidateId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(cs => cs.Skill)
            .WithMany(s => s.CandidateSkills)
            .HasForeignKey(cs => cs.SkillId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(cs => new { cs.CandidateId, cs.SkillId })
            .IsUnique();

        builder.HasIndex(cs => cs.CompanyId);
    }
}
