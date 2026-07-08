using System;
using System.Collections.Generic;
using RecruitIQ.Domain.Base;
using RecruitIQ.Domain.Enums;

namespace RecruitIQ.Domain.Entities;

public class Job : BaseEntity, IMultiTenant
{
    public Guid CompanyId { get; set; }
    public Guid DepartmentId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Requirements { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public EmploymentType EmploymentType { get; set; }
    public JobStatus Status { get; set; }
    public decimal? SalaryMin { get; set; }
    public decimal? SalaryMax { get; set; }
    public Guid? HiringManagerId { get; set; }
    public string JobCode { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public DateTime? PublishedAt { get; set; }
    public DateTime? ClosingDate { get; set; }

    // Navigation properties
    public virtual Company Company { get; set; } = null!;
    public virtual Department Department { get; set; } = null!;
    public virtual User? HiringManager { get; set; }
    public virtual ICollection<JobVersion> JobVersions { get; set; } = new List<JobVersion>();
    public virtual ICollection<JobSkill> JobSkills { get; set; } = new List<JobSkill>();
    public virtual ICollection<Application> Applications { get; set; } = new List<Application>();
    public virtual ICollection<AIAnalysis> AIAnalyses { get; set; } = new List<AIAnalysis>();
}
