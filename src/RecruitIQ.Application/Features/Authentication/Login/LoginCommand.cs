using MediatR;
using RecruitIQ.Common;
using RecruitIQ.Contracts;

namespace RecruitIQ.Application.Features.Authentication.Login;

public record LoginCommand(string Email, string Password, string IpAddress, string UserAgent) : IRequest<Result<AuthResponse>>;
