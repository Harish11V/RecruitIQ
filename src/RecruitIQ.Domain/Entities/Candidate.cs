using System;
using System.Collections.Generic;
using RecruitIQ.Domain.Base;
using RecruitIQ.Domain.Enums;

namespace RecruitIQ.Domain.Entities;

public class Candidate : BaseEntity, IMultiTenant
{
    public Guid CompanyId { get; set; }
    public string CandidateNumber { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public string? LinkedInUrl { get; set; }
    public string? Title { get; set; }
    public CandidateStatus Status { get; set; } = CandidateStatus.New;
    public int? YearsOfExperience { get; set; }

    // Navigation properties
    public virtual Company Company { get; set; } = null!;
    public virtual ICollection<Resume> Resumes { get; set; } = new List<Resume>();
    public virtual ICollection<CandidateExperience> Experiences { get; set; } = new List<CandidateExperience>();
    public virtual ICollection<CandidateEducation> Educations { get; set; } = new List<CandidateEducation>();
    public virtual ICollection<CandidateCertification> Certifications { get; set; } = new List<CandidateCertification>();
    public virtual ICollection<CandidateSkill> CandidateSkills { get; set; } = new List<CandidateSkill>();
    public virtual ICollection<Application> Applications { get; set; } = new List<Application>();
}
