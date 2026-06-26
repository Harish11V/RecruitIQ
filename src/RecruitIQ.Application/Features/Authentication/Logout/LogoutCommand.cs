using MediatR;
using RecruitIQ.Common;

namespace RecruitIQ.Application.Features.Authentication.Logout;

public record LogoutCommand(string RefreshToken, string IpAddress) : IRequest<Result>;
