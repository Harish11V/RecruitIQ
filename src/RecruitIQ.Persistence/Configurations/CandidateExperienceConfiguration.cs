using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RecruitIQ.Domain.Entities;
using RecruitIQ.Persistence.Configurations.Base;

namespace RecruitIQ.Persistence.Configurations;

public class CandidateExperienceConfiguration : BaseEntityConfiguration<CandidateExperience>
{
    public override void Configure(EntityTypeBuilder<CandidateExperience> builder)
    {
        base.Configure(builder);

        builder.Property(ce => ce.CompanyName)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(ce => ce.Role)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(ce => ce.Description);

        builder.HasOne(ce => ce.Company)
            .WithMany()
            .HasForeignKey(ce => ce.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(ce => ce.Candidate)
            .WithMany(c => c.Experiences)
            .HasForeignKey(ce => ce.CandidateId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(ce => ce.CompanyId);
    }
}
