using System.Collections.Generic;
using System.Security.Claims;
using RecruitIQ.Domain.Entities;

namespace RecruitIQ.Application.Common.Interfaces;

public interface IJwtTokenGenerator
{
    string GenerateToken(User user, IEnumerable<string> roles);
    string GenerateRefreshToken();
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
}
