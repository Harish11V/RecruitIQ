namespace RecruitIQ.Contracts;

public record UpdateDepartmentRequest(
    string Name,
    string? Description,
    byte[] RowVersion);
