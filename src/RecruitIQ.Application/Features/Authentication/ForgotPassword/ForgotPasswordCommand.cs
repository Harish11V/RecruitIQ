using MediatR;
using RecruitIQ.Common;

namespace RecruitIQ.Application.Features.Authentication.ForgotPassword;

public record ForgotPasswordCommand(string Email) : IRequest<Result>;
