using System;
using System.Collections.Generic;
using RecruitIQ.Domain.Base;

namespace RecruitIQ.Domain.Entities;

public class Application : BaseEntity, IMultiTenant
{
    public Guid CompanyId { get; set; }
    public Guid JobId { get; set; }
    public Guid CandidateId { get; set; }
    public Guid CurrentStageId { get; set; }
    public DateTime AppliedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Company Company { get; set; } = null!;
    public virtual Job Job { get; set; } = null!;
    public virtual Candidate Candidate { get; set; } = null!;
    public virtual HiringStage CurrentStage { get; set; } = null!;
    public virtual ICollection<Interview> Interviews { get; set; } = new List<Interview>();
}
