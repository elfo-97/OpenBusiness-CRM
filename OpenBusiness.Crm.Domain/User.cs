namespace OpenBusiness.Crm.Domain;

public sealed class User
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string Email { get; private set; } = default!;
    public string FullName { get; private set; } = default!;
    public string PasswordHash { get; private set; } = default!;
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? LastLoginAt { get; private set; }

    private User() { }

    public User(Guid tenantId, string email, string fullName, string passwordHash)
    {
        Id = Guid.NewGuid();
        TenantId = tenantId;
        Email = email;
        FullName = fullName;
        PasswordHash = passwordHash;
        IsActive = true;
        CreatedAt = DateTimeOffset.UtcNow;
    }

    public void UpdateLastLogin() => LastLoginAt = DateTimeOffset.UtcNow;
    public void Deactivate() => IsActive = false;
}