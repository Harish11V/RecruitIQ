using System.Collections.Generic;
using RecruitIQ.Domain.Base;

namespace RecruitIQ.Domain.Entities;

public class Company : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Subdomain { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public virtual CompanySettings? Settings { get; set; }
    public virtual ICollection<Department> Departments { get; set; } = new List<Department>();
    public virtual ICollection<User> Users { get; set; } = new List<User>();
    public virtual ICollection<HiringStage> HiringStages { get; set; } = new List<HiringStage>();
    public virtual ICollection<Job> Jobs { get; set; } = new List<Job>();
    public virtual ICollection<Candidate> Candidates { get; set; } = new List<Candidate>();
    public virtual ICollection<Application> Applications { get; set; } = new List<Application>();
}
