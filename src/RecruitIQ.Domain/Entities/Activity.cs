using System;
using RecruitIQ.Domain.Base;

namespace RecruitIQ.Domain.Entities;

public class Activity : BaseEntity, IMultiTenant
{
    public Guid CompanyId { get; set; }
    public Guid? UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Company Company { get; set; } = null!;
    public virtual User? User { get; set; }
}
