using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RecruitIQ.Domain.Entities;
using RecruitIQ.Persistence.Configurations.Base;

namespace RecruitIQ.Persistence.Configurations;

public class ActivityConfiguration : BaseEntityConfiguration<Activity>
{
    public override void Configure(EntityTypeBuilder<Activity> builder)
    {
        base.Configure(builder);

        builder.Property(act => act.Action)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(act => act.EntityName)
            .HasMaxLength(50)
            .IsRequired();

        builder.HasOne(act => act.Company)
            .WithMany()
            .HasForeignKey(act => act.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(act => act.User)
            .WithMany()
            .HasForeignKey(act => act.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(act => act.CompanyId);
    }
}
