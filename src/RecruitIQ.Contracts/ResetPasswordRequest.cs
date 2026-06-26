namespace RecruitIQ.Contracts;

public record ResetPasswordRequest(string Email, string ResetToken, string NewPassword);
