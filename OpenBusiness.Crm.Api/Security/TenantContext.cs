namespace OpenBusiness.Crm.Api.Security;

public sealed class TenantContext
{
    public Guid TenantId { get; private set; }
    public Guid UserId { get; private set; }
    public string Email { get; private set; } = default!;
    public string FullName { get; private set; } = default!;
    public bool IsAuthenticated { get; private set; }

    private TenantContext() { }

    public static TenantContext Empty => new TenantContext { IsAuthenticated = false };

    public static TenantContext Create(Guid tenantId, Guid userId, string email, string fullName)
        => new TenantContext
        {
            TenantId = tenantId,
            UserId = userId,
            Email = email,
            FullName = fullName,
            IsAuthenticated = true
        };
}