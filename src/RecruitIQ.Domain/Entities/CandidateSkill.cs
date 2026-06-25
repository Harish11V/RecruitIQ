using System;
using RecruitIQ.Domain.Base;
using RecruitIQ.Domain.Enums;

namespace RecruitIQ.Domain.Entities;

public class CandidateSkill : BaseEntity, IMultiTenant
{
    public Guid CompanyId { get; set; }
    public Guid CandidateId { get; set; }
    public Guid SkillId { get; set; }
    public CandidateProficiency ProficiencyLevel { get; set; }

    // Navigation properties
    public virtual Company Company { get; set; } = null!;
    public virtual Candidate Candidate { get; set; } = null!;
    public virtual Skill Skill { get; set; } = null!;
}
