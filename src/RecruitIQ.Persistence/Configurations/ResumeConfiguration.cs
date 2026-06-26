using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RecruitIQ.Domain.Entities;
using RecruitIQ.Persistence.Configurations.Base;

namespace RecruitIQ.Persistence.Configurations;

public class ResumeConfiguration : BaseEntityConfiguration<Resume>
{
    public override void Configure(EntityTypeBuilder<Resume> builder)
    {
        base.Configure(builder);

        builder.Property(r => r.FileName)
            .HasMaxLength(250)
            .IsRequired();

        builder.Property(r => r.StoragePath)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(r => r.AIExtractedText);

        builder.Property(r => r.AISummary);

        builder.HasOne(r => r.Company)
            .WithMany()
            .HasForeignKey(r => r.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Candidate)
            .WithMany(c => c.Resumes)
            .HasForeignKey(r => r.CandidateId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(r => new { r.CompanyId, r.CandidateId, r.UploadedDate });
        builder.HasIndex(r => r.CompanyId);
    }
}
