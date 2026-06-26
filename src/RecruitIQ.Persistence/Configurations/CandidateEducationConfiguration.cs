using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RecruitIQ.Domain.Entities;
using RecruitIQ.Persistence.Configurations.Base;

namespace RecruitIQ.Persistence.Configurations;

public class CandidateEducationConfiguration : BaseEntityConfiguration<CandidateEducation>
{
    public override void Configure(EntityTypeBuilder<CandidateEducation> builder)
    {
        base.Configure(builder);

        builder.Property(ce => ce.SchoolOrCollege)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(ce => ce.Degree)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(ce => ce.GPA)
            .HasColumnType("decimal(3,2)");

        builder.HasOne(ce => ce.Company)
            .WithMany()
            .HasForeignKey(ce => ce.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(ce => ce.Candidate)
            .WithMany(c => c.Educations)
            .HasForeignKey(ce => ce.CandidateId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(ce => ce.CompanyId);
    }
}
