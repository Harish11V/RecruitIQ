using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RecruitIQ.Domain.Entities;
using RecruitIQ.Persistence.Configurations.Base;

namespace RecruitIQ.Persistence.Configurations;

public class JobConfiguration : BaseEntityConfiguration<Job>
{
    public override void Configure(EntityTypeBuilder<Job> builder)
    {
        base.Configure(builder);

        builder.Property(j => j.Title)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(j => j.Description)
            .IsRequired();

        builder.Property(j => j.Requirements)
            .IsRequired();

        builder.Property(j => j.Location)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(j => j.EmploymentType)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(j => j.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(j => j.SalaryMin)
            .HasColumnType("decimal(18,2)");

        builder.Property(j => j.SalaryMax)
            .HasColumnType("decimal(18,2)");

        builder.Property(j => j.JobCode)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(j => j.Slug)
            .HasMaxLength(250)
            .IsRequired();

        builder.Property(j => j.PublishedAt)
            .HasColumnType("datetime2");

        builder.Property(j => j.ClosingDate)
            .HasColumnType("datetime2");

        builder.HasOne(j => j.Company)
            .WithMany(c => c.Jobs)
            .HasForeignKey(j => j.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(j => j.Department)
            .WithMany(d => d.Jobs)
            .HasForeignKey(j => j.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(j => j.HiringManager)
            .WithMany()
            .HasForeignKey(j => j.HiringManagerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(j => new { j.CompanyId, j.Status, j.DepartmentId, j.CreatedAt });
        builder.HasIndex(j => j.CompanyId);
    }
}
