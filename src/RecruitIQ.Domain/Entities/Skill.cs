using System.Collections.Generic;
using RecruitIQ.Domain.Base;

namespace RecruitIQ.Domain.Entities;

public class Skill : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    // Navigation properties
    public virtual ICollection<JobSkill> JobSkills { get; set; } = new List<JobSkill>();
    public virtual ICollection<CandidateSkill> CandidateSkills { get; set; } = new List<CandidateSkill>();
}
