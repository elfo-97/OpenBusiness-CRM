namespace OpenBusiness.Crm.Domain;

public enum LeadStage
{
    New = 1,
    Qualified = 2,
    Proposal = 3,
    Negotiation = 4,
    Won = 5,
    Lost = 6
}

public sealed class Lead
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid? CustomerId { get; private set; }
    public string Title { get; private set; } = default!;
    public string? Description { get; private set; }
    public LeadStage Stage { get; private set; }
    public decimal? EstimatedValue { get; private set; }
    public DateTimeOffset? ExpectedCloseDate { get; private set; }
    public Guid? AssignedToUserId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }
    public DateTimeOffset? ClosedAt { get; private set; }

    private Lead() { }

    public Lead(Guid tenantId, string title, string? description = null, Guid? customerId = null, decimal? estimatedValue = null, DateTimeOffset? expectedCloseDate = null, Guid? assignedToUserId = null)
    {
        Id = Guid.NewGuid();
        TenantId = tenantId;
        Title = title;
        Description = description;
        CustomerId = customerId;
        Stage = LeadStage.New;
        EstimatedValue = estimatedValue;
        ExpectedCloseDate = expectedCloseDate;
        AssignedToUserId = assignedToUserId;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public void Update(string title, string? description, Guid? customerId, decimal? estimatedValue, DateTimeOffset? expectedCloseDate, Guid? assignedToUserId)
    {
        Title = title;
        Description = description;
        CustomerId = customerId;
        EstimatedValue = estimatedValue;
        ExpectedCloseDate = expectedCloseDate;
        AssignedToUserId = assignedToUserId;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void ChangeStage(LeadStage newStage)
    {
        Stage = newStage;
        UpdatedAt = DateTimeOffset.UtcNow;
        if (newStage == LeadStage.Won || newStage == LeadStage.Lost)
        {
            ClosedAt = DateTimeOffset.UtcNow;
        }
    }

    public void Deactivate() => ChangeStage(LeadStage.Lost);
}