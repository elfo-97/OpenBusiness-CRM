namespace OpenBusiness.Crm.Domain;

public sealed class Contact
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid CustomerId { get; private set; }
    public string Name { get; private set; } = default!;
    public string? Email { get; private set; }
    public string? Phone { get; private set; }
    public string? Role { get; private set; }
    public bool IsPrimary { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    private Contact() { }

    public Contact(Guid tenantId, Guid customerId, string name, string? email = null, string? phone = null, string? role = null, bool isPrimary = false)
    {
        Id = Guid.NewGuid();
        TenantId = tenantId;
        CustomerId = customerId;
        Name = name;
        Email = email;
        Phone = phone;
        Role = role;
        IsPrimary = isPrimary;
        IsActive = true;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public void Update(string name, string? email, string? phone, string? role, bool isPrimary)
    {
        Name = name;
        Email = email;
        Phone = phone;
        Role = role;
        IsPrimary = isPrimary;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Deactivate() => IsActive = false;
}