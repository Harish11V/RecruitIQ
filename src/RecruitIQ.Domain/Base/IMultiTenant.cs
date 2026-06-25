using System;

namespace RecruitIQ.Domain.Base;

public interface IMultiTenant
{
    Guid CompanyId { get; set; }
}
