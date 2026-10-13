namespace OpenBusiness.Crm.Application.Leads;

public sealed record CreateLeadRequest(
    string Title,
    string? Description = null,
    Guid? CustomerId = null,
    decimal? EstimatedValue = null,
    DateTimeOffset? ExpectedCloseDate = null,
    Guid? AssignedToUserId = null
);

public sealed record UpdateLeadRequest(
    string Title,
    string? Description = null,
    Guid? CustomerId = null,
    decimal? EstimatedValue = null,
    DateTimeOffset? ExpectedCloseDate = null,
    Guid? AssignedToUserId = null
);

public sealed record ChangeLeadStageRequest(
    Domain.LeadStage Stage
);

public sealed record LeadResponse(
    Guid Id,
    Guid? CustomerId,
    string Title,
    string? Description,
    Domain.LeadStage Stage,
    decimal? EstimatedValue,
    DateTimeOffset? ExpectedCloseDate,
    Guid? AssignedToUserId,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt,
    DateTimeOffset? ClosedAt
);