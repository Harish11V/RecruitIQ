using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RecruitIQ.Domain.Entities;
using RecruitIQ.Persistence.Configurations.Base;

namespace RecruitIQ.Persistence.Configurations;

public class JobVersionConfiguration : BaseEntityConfiguration<JobVersion>
{
    public override void Configure(EntityTypeBuilder<JobVersion> builder)
    {
        base.Configure(builder);

        builder.Property(jv => jv.Title)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(jv => jv.Description)
            .IsRequired();

        builder.Property(jv => jv.Requirements)
            .IsRequired();

        builder.Property(jv => jv.ModifiedBy)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasOne(jv => jv.Company)
            .WithMany()
            .HasForeignKey(jv => jv.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(jv => jv.Job)
            .WithMany(j => j.JobVersions)
            .HasForeignKey(jv => jv.JobId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(jv => jv.CompanyId);
    }
}
