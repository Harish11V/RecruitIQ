using System;
using System.Collections.Generic;
using RecruitIQ.Domain.Base;

namespace RecruitIQ.Domain.Entities;

public class Department : BaseEntity, IMultiTenant
{
    public Guid CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    // Navigation properties
    public virtual Company Company { get; set; } = null!;
    public virtual ICollection<Job> Jobs { get; set; } = new List<Job>();
}
