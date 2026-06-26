using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RecruitIQ.Domain.Entities;
using RecruitIQ.Persistence.Configurations.Base;

namespace RecruitIQ.Persistence.Configurations;

public class CandidateCertificationConfiguration : BaseEntityConfiguration<CandidateCertification>
{
    public override void Configure(EntityTypeBuilder<CandidateCertification> builder)
    {
        base.Configure(builder);

        builder.Property(cc => cc.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(cc => cc.IssuingAuthority)
            .HasMaxLength(150)
            .IsRequired();

        builder.HasOne(cc => cc.Company)
            .WithMany()
            .HasForeignKey(cc => cc.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(cc => cc.Candidate)
            .WithMany(c => c.Certifications)
            .HasForeignKey(cc => cc.CandidateId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(cc => cc.CompanyId);
    }
}
