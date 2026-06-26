using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RecruitIQ.Domain.Entities;
using RecruitIQ.Persistence.Configurations.Base;

namespace RecruitIQ.Persistence.Configurations;

public class InterviewConfiguration : BaseEntityConfiguration<Interview>
{
    public override void Configure(EntityTypeBuilder<Interview> builder)
    {
        base.Configure(builder);

        builder.Property(i => i.Title)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(i => i.LocationOrLink)
            .HasMaxLength(500);

        builder.Property(i => i.Status)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.HasOne(i => i.Company)
            .WithMany()
            .HasForeignKey(i => i.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.Application)
            .WithMany(a => a.Interviews)
            .HasForeignKey(i => i.ApplicationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(i => new { i.CompanyId, i.ApplicationId, i.ScheduledAt });
        builder.HasIndex(i => i.CompanyId);
    }
}
