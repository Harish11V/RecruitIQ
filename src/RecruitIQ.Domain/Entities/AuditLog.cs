using System;
using RecruitIQ.Domain.Base;

namespace RecruitIQ.Domain.Entities;

public class AuditLog : BaseEntity, IMultiTenant
{
    public Guid CompanyId { get; set; }
    public Guid? UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string TableName { get; set; } = string.Empty;
    public Guid RecordId { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Company Company { get; set; } = null!;
    public virtual User? User { get; set; }
}
