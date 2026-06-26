using System;

namespace RecruitIQ.Contracts;

public record AuthResponse(string AccessToken, string RefreshToken, DateTime AccessTokenExpiry);
