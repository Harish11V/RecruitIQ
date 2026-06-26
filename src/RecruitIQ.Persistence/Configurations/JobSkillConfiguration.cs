using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RecruitIQ.Domain.Entities;
using RecruitIQ.Persistence.Configurations.Base;

namespace RecruitIQ.Persistence.Configurations;

public class JobSkillConfiguration : BaseEntityConfiguration<JobSkill>
{
    public override void Configure(EntityTypeBuilder<JobSkill> builder)
    {
        base.Configure(builder);

        builder.HasOne(js => js.Company)
            .WithMany()
            .HasForeignKey(js => js.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(js => js.Job)
            .WithMany(j => j.JobSkills)
            .HasForeignKey(js => js.JobId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(js => js.Skill)
            .WithMany(s => s.JobSkills)
            .HasForeignKey(js => js.SkillId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(js => new { js.JobId, js.SkillId })
            .IsUnique();

        builder.HasIndex(js => js.CompanyId);
    }
}
