using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RecruitIQ.Domain.Entities;
using RecruitIQ.Persistence.Configurations.Base;

namespace RecruitIQ.Persistence.Configurations;

public class AuditLogConfiguration : BaseEntityConfiguration<AuditLog>
{
    public override void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        base.Configure(builder);

        builder.Property(al => al.Action)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(al => al.TableName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(al => al.OldValues);

        builder.Property(al => al.NewValues);

        builder.HasOne(al => al.Company)
            .WithMany()
            .HasForeignKey(al => al.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(al => al.User)
            .WithMany()
            .HasForeignKey(al => al.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(al => new { al.CompanyId, al.TableName, al.RecordId });
    }
}
