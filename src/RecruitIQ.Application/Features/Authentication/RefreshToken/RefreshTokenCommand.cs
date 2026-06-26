using MediatR;
using RecruitIQ.Common;
using RecruitIQ.Contracts;

namespace RecruitIQ.Application.Features.Authentication.RefreshToken;

public record RefreshTokenCommand(string AccessToken, string RefreshToken, string IpAddress) : IRequest<Result<AuthResponse>>;
