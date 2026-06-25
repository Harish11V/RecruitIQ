using System;
using RecruitIQ.Domain.Base;
using RecruitIQ.Domain.Enums;

namespace RecruitIQ.Domain.Entities;

public class InterviewFeedback : BaseEntity, IMultiTenant
{
    public Guid CompanyId { get; set; }
    public Guid InterviewId { get; set; }
    public Guid InterviewerId { get; set; }
    public int CommunicationScore { get; set; }
    public int TechnicalScore { get; set; }
    public int ProblemSolvingScore { get; set; }
    public string Comments { get; set; } = string.Empty;
    public RecommendationType Recommendation { get; set; }

    // Navigation properties
    public virtual Company Company { get; set; } = null!;
    public virtual Interview Interview { get; set; } = null!;
    public virtual User Interviewer { get; set; } = null!;
}
