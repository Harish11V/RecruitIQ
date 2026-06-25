using System;
using RecruitIQ.Domain.Base;

namespace RecruitIQ.Domain.Entities;

public class CandidateEducation : BaseEntity, IMultiTenant
{
    public Guid CompanyId { get; set; }
    public Guid CandidateId { get; set; }
    public string SchoolOrCollege { get; set; } = string.Empty;
    public string Degree { get; set; } = string.Empty;
    public decimal? GPA { get; set; }
    public int StartYear { get; set; }
    public int? EndYear { get; set; }

    // Navigation properties
    public virtual Company Company { get; set; } = null!;
    public virtual Candidate Candidate { get; set; } = null!;
}
