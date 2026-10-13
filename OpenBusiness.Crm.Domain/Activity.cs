namespace OpenBusiness.Crm.Domain;

public enum ActivityType
{
    Note = 1,
    Call = 2,
    Meeting = 3,
    Task = 4,
    Email = 5
}

public sealed class Activity
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid? CustomerId { get; private set; }
    public Guid? LeadId { get; private set; }
    public Guid UserId { get; private set; }
    public ActivityType Type { get; private set; }
    public string Subject { get; private set; } = default!;
    public string? Description { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    private Activity() { }

    public Activity(Guid tenantId, Guid userId, ActivityType type, string subject, string? description = null, Guid? customerId = null, Guid? leadId = null, DateTimeOffset? occurredAt = null)
    {
        Id = Guid.NewGuid();
        TenantId = tenantId;
        UserId = userId;
        Type = type;
        Subject = subject;
        Description = description;
        CustomerId = customerId;
        LeadId = leadId;
        OccurredAt = occurredAt ?? DateTimeOffset.UtcNow;
        CreatedAt = DateTimeOffset.UtcNow;
    }
}