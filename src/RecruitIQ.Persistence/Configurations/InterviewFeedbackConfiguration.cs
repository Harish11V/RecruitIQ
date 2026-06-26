using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RecruitIQ.Domain.Entities;
using RecruitIQ.Persistence.Configurations.Base;

namespace RecruitIQ.Persistence.Configurations;

public class InterviewFeedbackConfiguration : BaseEntityConfiguration<InterviewFeedback>
{
    public override void Configure(EntityTypeBuilder<InterviewFeedback> builder)
    {
        base.Configure(builder);

        builder.Property(ifb => ifb.Comments)
            .IsRequired();

        builder.Property(ifb => ifb.Recommendation)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.HasOne(ifb => ifb.Company)
            .WithMany()
            .HasForeignKey(ifb => ifb.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(ifb => ifb.Interview)
            .WithMany(i => i.Feedbacks)
            .HasForeignKey(ifb => ifb.InterviewId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(ifb => ifb.Interviewer)
            .WithMany(u => u.InterviewFeedbacks)
            .HasForeignKey(ifb => ifb.InterviewerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(ifb => new { ifb.InterviewId, ifb.InterviewerId })
            .IsUnique();

        builder.HasIndex(ifb => ifb.CompanyId);
    }
}
