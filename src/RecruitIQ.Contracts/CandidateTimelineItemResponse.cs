using System;

namespace RecruitIQ.Contracts;

public record CandidateTimelineItemResponse(
    Guid ActivityId,
    DateTime Timestamp,
    string Action,
    string? Description,
    string PerformedBy,
    string Icon,
    string Color,
    string? Metadata);
