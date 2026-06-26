using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RecruitIQ.Domain.Entities;
using RecruitIQ.Persistence.Configurations.Base;

namespace RecruitIQ.Persistence.Configurations;

public class HiringStageConfiguration : BaseEntityConfiguration<HiringStage>
{
    public override void Configure(EntityTypeBuilder<HiringStage> builder)
    {
        base.Configure(builder);

        builder.Property(hs => hs.Name)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(hs => hs.SequenceOrder)
            .IsRequired();

        builder.HasOne(hs => hs.Company)
            .WithMany(c => c.HiringStages)
            .HasForeignKey(hs => hs.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(hs => new { hs.CompanyId, hs.SequenceOrder })
            .IsUnique();

        builder.HasIndex(hs => hs.CompanyId);
    }
}
