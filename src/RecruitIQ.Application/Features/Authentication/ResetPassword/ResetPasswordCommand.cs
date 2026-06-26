using MediatR;
using RecruitIQ.Common;

namespace RecruitIQ.Application.Features.Authentication.ResetPassword;

public record ResetPasswordCommand(string Email, string ResetToken, string NewPassword) : IRequest<Result>;
