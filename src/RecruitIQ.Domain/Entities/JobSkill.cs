using System;
using RecruitIQ.Domain.Base;

namespace RecruitIQ.Domain.Entities;

public class JobSkill : BaseEntity, IMultiTenant
{
    public Guid CompanyId { get; set; }
    public Guid JobId { get; set; }
    public Guid SkillId { get; set; }
    public bool IsRequired { get; set; }

    // Navigation properties
    public virtual Company Company { get; set; } = null!;
    public virtual Job Job { get; set; } = null!;
    public virtual Skill Skill { get; set; } = null!;
}
