using System;
using RecruitIQ.Domain.Base;

namespace RecruitIQ.Domain.Entities;

public class CompanySettings : BaseEntity, IMultiTenant
{
    public Guid CompanyId { get; set; }
    public string Theme { get; set; } = "Light";
    public string? LogoUrl { get; set; }
    public string Timezone { get; set; } = "UTC";
    public int DefaultInterviewDuration { get; set; } = 30;
    public string? AllowedEmailDomain { get; set; }

    // Navigation properties
    public virtual Company Company { get; set; } = null!;
}
