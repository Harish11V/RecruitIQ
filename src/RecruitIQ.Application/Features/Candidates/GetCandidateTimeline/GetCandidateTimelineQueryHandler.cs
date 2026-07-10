using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.EntityFrameworkCore;
using RecruitIQ.Application.Common.Interfaces;
using RecruitIQ.Common;
using RecruitIQ.Contracts;
using RecruitIQ.Domain.Entities;

namespace RecruitIQ.Application.Features.Candidates.GetCandidateTimeline;

public class GetCandidateTimelineQueryHandler : IRequestHandler<GetCandidateTimelineQuery, Result<IReadOnlyList<CandidateTimelineItemResponse>>>
{
    private readonly IRecruitIQDbContext _context;
    private readonly ITenantService _tenantService;

    public GetCandidateTimelineQueryHandler(IRecruitIQDbContext context, ITenantService tenantService)
    {
        _context = context;
        _tenantService = tenantService;
    }

    public async Task<Result<IReadOnlyList<CandidateTimelineItemResponse>>> Handle(GetCandidateTimelineQuery request, CancellationToken cancellationToken)
    {
        var companyId = _tenantService.CompanyId;

        // 1. Fetch activities for the candidate under active company/tenant
        var activities = await _context.QueryReadOnly<Activity>()
            .Include(a => a.User)
            .Where(a => a.CompanyId == companyId && a.EntityId == request.CandidateId && a.EntityName == "Candidate" && !a.IsDeleted)
            .OrderByDescending(a => a.Timestamp)
            .ToListAsync(cancellationToken);

        // 2. Map and parse to timeline responses
        var timelineItems = activities.Select(a =>
        {
            // E.g. "Candidate Updated: John Smith. Changed fields: Status"
            // Split Action and Description on the first colon if available
            var actionTitle = a.Action;
            string? descriptionText = null;
            var colonIndex = a.Action.IndexOf(':');
            if (colonIndex > 0)
            {
                actionTitle = a.Action[..colonIndex].Trim();
                descriptionText = a.Action[(colonIndex + 1)..].Trim();
            }

            // Resolve performer name
            var performedBy = "System";
            if (a.User != null)
            {
                var fullName = $"{a.User.FirstName} {a.User.LastName}".Trim();
                performedBy = string.IsNullOrEmpty(fullName) ? a.User.Email : fullName;
            }

            // Resolve icon and color based on activity text
            var (icon, color) = ResolveActivityVisuals(a.Action);

            return new CandidateTimelineItemResponse(
                a.Id,
                a.Timestamp,
                actionTitle,
                descriptionText,
                performedBy,
                icon,
                color,
                null); // Metadata can be null for now
        }).ToList();

        return Result<IReadOnlyList<CandidateTimelineItemResponse>>.Success(timelineItems);
    }

    private static (string Icon, string Color) ResolveActivityVisuals(string action)
    {
        var normalizedAction = action.ToLowerInvariant();

        if (normalizedAction.Contains("created"))
            return ("person_add", "blue");
        if (normalizedAction.Contains("uploaded"))
            return ("cloud_upload", "green");
        if (normalizedAction.Contains("deleted"))
            return ("delete", "red");
        if (normalizedAction.Contains("primary"))
            return ("star", "amber");
        if (normalizedAction.Contains("updated"))
        {
            if (normalizedAction.Contains("status"))
                return ("change_circle", "purple");
            return ("edit", "indigo");
        }

        return ("history", "gray");
    }
}
