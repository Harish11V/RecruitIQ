using System.Collections.Generic;

namespace RecruitIQ.Contracts;

public record InviteUserRequest(
    string Email,
    string FirstName,
    string LastName,
    List<string> Roles);
