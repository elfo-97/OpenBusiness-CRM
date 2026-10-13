namespace OpenBusiness.Crm.Domain;

public sealed class Customer
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string Name { get; private set; } = default!;
    public string? Email { get; private set; }
    public string? Phone { get; private set; }
    public string? Document { get; private set; }
    public string? Address { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? UpdatedAt { get; private set; }

    private Customer() { }

    public Customer(Guid tenantId, string name, string? email = null, string? phone = null, string? document = null, string? address = null)
    {
        Id = Guid.NewGuid();
        TenantId = tenantId;
        Name = name;
        Email = email;
        Phone = phone;
        Document = document;
        Address = address;
        IsActive = true;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public void Update(string name, string? email, string? phone, string? document, string? address)
    {
        Name = name;
        Email = email;
        Phone = phone;
        Document = document;
        Address = address;
        UpdatedAt = DateTimeOffset.UtcNow;
    }

    public void Deactivate() => IsActive = false;
}