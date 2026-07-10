using System.Collections.Generic;
using RecruitIQ.Application.Common.Interfaces;
using RecruitIQ.Domain.Enums;

namespace RecruitIQ.Application.Services;

public class CandidateLifecycleService : ICandidateLifecycleService
{
    private static readonly Dictionary<CandidateStatus, List<CandidateStatus>> TransitionRules = new()
    {
        { CandidateStatus.New, new() { CandidateStatus.Available, CandidateStatus.Inactive } },
        { CandidateStatus.Available, new() { CandidateStatus.Shortlisted, CandidateStatus.Inactive } },
        { CandidateStatus.Shortlisted, new() { CandidateStatus.Interviewing, CandidateStatus.Rejected } },
        { CandidateStatus.Interviewing, new() { CandidateStatus.Offered, CandidateStatus.Rejected } },
        { CandidateStatus.Offered, new() { CandidateStatus.Hired, CandidateStatus.Rejected } },
        { CandidateStatus.Hired, new() { CandidateStatus.Inactive } },
        { CandidateStatus.Rejected, new() { CandidateStatus.Available } },
        { CandidateStatus.Inactive, new() { CandidateStatus.Available } }
    };

    public bool IsTransitionAllowed(CandidateStatus currentStatus, CandidateStatus newStatus)
    {
        if (currentStatus == newStatus) return true;

        if (TransitionRules.TryGetValue(currentStatus, out var allowedStatuses))
        {
            return allowedStatuses.Contains(newStatus);
        }

        return false;
    }

    public IReadOnlyList<CandidateStatus> GetAllowedNextStatuses(CandidateStatus currentStatus)
    {
        if (TransitionRules.TryGetValue(currentStatus, out var allowedStatuses))
        {
            return allowedStatuses;
        }

        return System.Array.Empty<CandidateStatus>();
    }
}
