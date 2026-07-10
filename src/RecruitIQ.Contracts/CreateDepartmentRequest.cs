namespace RecruitIQ.Contracts;

public record CreateDepartmentRequest(
    string Name,
    string? Description);
