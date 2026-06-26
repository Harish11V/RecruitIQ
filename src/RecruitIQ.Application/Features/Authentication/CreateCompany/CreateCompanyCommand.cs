using System;
using MediatR;
using RecruitIQ.Common;

namespace RecruitIQ.Application.Features.Authentication.CreateCompany;

public record CreateCompanyCommand(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string CompanyName,
    string Subdomain) : IRequest<Result<Guid>>;
