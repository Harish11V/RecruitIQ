using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RecruitIQ.Domain.Entities;
using RecruitIQ.Persistence.Configurations.Base;

namespace RecruitIQ.Persistence.Configurations;

public class CompanySettingsConfiguration : BaseEntityConfiguration<CompanySettings>
{
    public override void Configure(EntityTypeBuilder<CompanySettings> builder)
    {
        base.Configure(builder);

        builder.Property(cs => cs.Theme)
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(cs => cs.LogoUrl)
            .HasMaxLength(500);

        builder.Property(cs => cs.Timezone)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(cs => cs.DefaultInterviewDuration)
            .IsRequired();

        builder.Property(cs => cs.AllowedEmailDomain)
            .HasMaxLength(100);

        builder.HasOne(cs => cs.Company)
            .WithOne(c => c.Settings)
            .HasForeignKey<CompanySettings>(cs => cs.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(cs => cs.CompanyId)
            .IsUnique();
    }
}
