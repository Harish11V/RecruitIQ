using System;
using RecruitIQ.Domain.Base;

namespace RecruitIQ.Domain.Entities;

public class Notification : BaseEntity, IMultiTenant
{
    public Guid CompanyId { get; set; }
    public Guid UserId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }

    // Navigation properties
    public virtual Company Company { get; set; } = null!;
    public virtual User User { get; set; } = null!;
}
