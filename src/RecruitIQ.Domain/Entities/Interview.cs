using System;
using System.Collections.Generic;
using RecruitIQ.Domain.Base;
using RecruitIQ.Domain.Enums;

namespace RecruitIQ.Domain.Entities;

public class Interview : BaseEntity, IMultiTenant
{
    public Guid CompanyId { get; set; }
    public Guid ApplicationId { get; set; }
    public string Title { get; set; } = string.Empty;
    public DateTime ScheduledAt { get; set; }
    public int DurationMinutes { get; set; } = 30;
    public string? LocationOrLink { get; set; }
    public InterviewStatus Status { get; set; }

    // Navigation properties
    public virtual Company Company { get; set; } = null!;
    public virtual Application Application { get; set; } = null!;
    public virtual ICollection<InterviewFeedback> Feedbacks { get; set; } = new List<InterviewFeedback>();
}
