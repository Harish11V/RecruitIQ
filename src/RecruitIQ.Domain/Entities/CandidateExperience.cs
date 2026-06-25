using System;
using RecruitIQ.Domain.Base;

namespace RecruitIQ.Domain.Entities;

public class CandidateExperience : BaseEntity, IMultiTenant
{
    public Guid CompanyId { get; set; }
    public Guid CandidateId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Description { get; set; }

    // Navigation properties
    public virtual Company Company { get; set; } = null!;
    public virtual Candidate Candidate { get; set; } = null!;
}
