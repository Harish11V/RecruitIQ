using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RecruitIQ.Domain.Entities;
using RecruitIQ.Persistence.Configurations.Base;

namespace RecruitIQ.Persistence.Configurations;

public class CandidateConfiguration : BaseEntityConfiguration<Candidate>
{
    public override void Configure(EntityTypeBuilder<Candidate> builder)
    {
        base.Configure(builder);

        builder.Property(c => c.CandidateNumber)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(c => c.FirstName)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(c => c.LastName)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(c => c.Email)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(c => c.PhoneNumber)
            .HasMaxLength(20);

        builder.Property(c => c.LinkedInUrl)
            .HasMaxLength(500);

        builder.Property(c => c.Title)
            .HasMaxLength(150);

        builder.Property(c => c.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(c => c.YearsOfExperience);

        builder.HasOne(c => c.Company)
            .WithMany(co => co.Candidates)
            .HasForeignKey(c => c.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(c => new { c.CompanyId, c.Email })
            .IsUnique();

        builder.HasIndex(c => new { c.CompanyId, c.CandidateNumber })
            .IsUnique();

        builder.HasIndex(c => c.CompanyId);
    }
}
