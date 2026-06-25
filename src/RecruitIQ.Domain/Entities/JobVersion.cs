using System;
using RecruitIQ.Domain.Base;

namespace RecruitIQ.Domain.Entities;

public class JobVersion : BaseEntity, IMultiTenant
{
    public Guid CompanyId { get; set; }
    public Guid JobId { get; set; }
    public int Version { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Requirements { get; set; } = string.Empty;
    public DateTime ModifiedAt { get; set; }
    public string ModifiedBy { get; set; } = string.Empty;

    // Navigation properties
    public virtual Company Company { get; set; } = null!;
    public virtual Job Job { get; set; } = null!;
}
