using System;
using RecruitIQ.Domain.Base;

namespace RecruitIQ.Domain.Entities;

public class PasswordResetToken : BaseEntity
{
    public Guid UserId { get; set; }
    public string TokenHash { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }

    // Navigation properties
    public virtual User User { get; set; } = null!;
}
