using System.Collections.Generic;
using RecruitIQ.Domain.Enums;

namespace RecruitIQ.Application.Common.Interfaces;

public interface ICandidateLifecycleService
{
    bool IsTransitionAllowed(CandidateStatus currentStatus, CandidateStatus newStatus);
    IReadOnlyList<CandidateStatus> GetAllowedNextStatuses(CandidateStatus currentStatus);
}
