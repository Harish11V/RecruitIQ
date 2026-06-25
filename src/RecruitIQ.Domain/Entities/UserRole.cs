using System;
using RecruitIQ.Domain.Base;

namespace RecruitIQ.Domain.Entities;

public class UserRole : BaseEntity, IMultiTenant
{
    public Guid CompanyId { get; set; }
    public Guid UserId { get; set; }
    public Guid RoleId { get; set; }

    // Navigation properties
    public virtual Company Company { get; set; } = null!;
    public virtual User User { get; set; } = null!;
    public virtual Role Role { get; set; } = null!;
}
