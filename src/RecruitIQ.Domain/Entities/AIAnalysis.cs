using System;
using RecruitIQ.Domain.Base;

namespace RecruitIQ.Domain.Entities;

public class AIAnalysis : BaseEntity, IMultiTenant
{
    public Guid CompanyId { get; set; }
    public Guid ResumeId { get; set; }
    public Guid JobId { get; set; }
    public int Score { get; set; }
    public string Strengths { get; set; } = string.Empty;
    public string Weaknesses { get; set; } = string.Empty;
    public string Recommendations { get; set; } = string.Empty;

    // Navigation properties
    public virtual Company Company { get; set; } = null!;
    public virtual Resume Resume { get; set; } = null!;
    public virtual Job Job { get; set; } = null!;
}
