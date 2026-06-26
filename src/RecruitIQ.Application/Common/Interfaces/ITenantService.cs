using System;

namespace RecruitIQ.Application.Common.Interfaces;

public interface ITenantService
{
    Guid CompanyId { get; }
}
