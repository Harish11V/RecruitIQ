using System;
using System.Collections.Generic;
using RecruitIQ.Domain.Base;

namespace RecruitIQ.Domain.Entities;

public class Resume : BaseEntity, IMultiTenant
{
    public Guid CompanyId { get; set; }
    public Guid CandidateId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string StoragePath { get; set; } = string.Empty;
    public DateTime UploadedDate { get; set; } = DateTime.UtcNow;
    public int Version { get; set; } = 1;
    public string? AIExtractedText { get; set; }
    public string? AISummary { get; set; }

    // Navigation properties
    public virtual Company Company { get; set; } = null!;
    public virtual Candidate Candidate { get; set; } = null!;
    public virtual ICollection<AIAnalysis> AIAnalyses { get; set; } = new List<AIAnalysis>();
}
