using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RecruitIQ.Domain.Entities;
using RecruitIQ.Persistence.Configurations.Base;

namespace RecruitIQ.Persistence.Configurations;

public class PasswordResetTokenConfiguration : BaseEntityConfiguration<PasswordResetToken>
{
    public override void Configure(EntityTypeBuilder<PasswordResetToken> builder)
    {
        base.Configure(builder);

        builder.Property(pr => pr.TokenHash)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(pr => pr.ExpiresAt)
            .HasColumnType("datetime2")
            .IsRequired();

        builder.Property(pr => pr.UsedAt)
            .HasColumnType("datetime2");

        builder.HasOne(pr => pr.User)
            .WithMany()
            .HasForeignKey(pr => pr.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(pr => pr.TokenHash)
            .IsUnique();
    }
}
