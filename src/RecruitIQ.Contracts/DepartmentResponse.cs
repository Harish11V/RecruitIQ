using System;

namespace RecruitIQ.Contracts;

public record DepartmentResponse(
    Guid Id,
    string Name,
    string? Description,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    byte[] RowVersion);
