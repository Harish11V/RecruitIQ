using System;
using System.Collections.Generic;
using RecruitIQ.Domain.Base;

namespace RecruitIQ.Domain.Entities;

public class HiringStage : BaseEntity, IMultiTenant
{
    public Guid CompanyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int SequenceOrder { get; set; }

    // Navigation properties
    public virtual Company Company { get; set; } = null!;
    public virtual ICollection<Application> Applications { get; set; } = new List<Application>();
}
