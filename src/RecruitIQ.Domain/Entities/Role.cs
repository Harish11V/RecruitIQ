using System.Collections.Generic;
using RecruitIQ.Domain.Base;

namespace RecruitIQ.Domain.Entities;

public class Role : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    // Navigation properties
    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
