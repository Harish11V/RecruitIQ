using System;
using RecruitIQ.Domain.Base;

namespace RecruitIQ.Domain.Entities;

public class CandidateCertification : BaseEntity, IMultiTenant
{
    public Guid CompanyId { get; set; }
    public Guid CandidateId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string IssuingAuthority { get; set; } = string.Empty;
    public DateTime IssueDate { get; set; }
    public DateTime? ExpirationDate { get; set; }

    // Navigation properties
    public virtual Company Company { get; set; } = null!;
    public virtual Candidate Candidate { get; set; } = null!;
}
