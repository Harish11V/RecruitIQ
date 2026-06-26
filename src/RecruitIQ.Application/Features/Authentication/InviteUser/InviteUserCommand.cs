using System;
using System.Collections.Generic;
using MediatR;
using RecruitIQ.Common;

namespace RecruitIQ.Application.Features.Authentication.InviteUser;

public record InviteUserCommand(
    string Email,
    string FirstName,
    string LastName,
    List<string> Roles) : IRequest<Result<Guid>>;
