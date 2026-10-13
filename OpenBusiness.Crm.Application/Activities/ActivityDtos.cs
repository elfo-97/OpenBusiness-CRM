namespace OpenBusiness.Crm.Application.Activities;

public sealed record CreateActivityRequest(
    Domain.ActivityType Type,
    string Subject,
    string? Description = null,
    Guid? CustomerId = null,
    Guid? LeadId = null,
    DateTimeOffset? OccurredAt = null
);

public sealed record ActivityResponse(
    Guid Id,
    Guid? CustomerId,
    Guid? LeadId,
    Guid UserId,
    Domain.ActivityType Type,
    string Subject,
    string? Description,
    DateTimeOffset OccurredAt,
    DateTimeOffset CreatedAt
);