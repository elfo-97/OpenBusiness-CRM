using Microsoft.AspNetCore.Identity;
using OpenBusiness.Crm.Domain;

namespace OpenBusiness.Crm.Api.Data;

public enum UserRole
{
    Member = 1,
    Admin = 2,
    Owner = 3
}

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public Guid TenantId { get; set; }
    public string FullName { get; set; } = default!;
    public UserRole Role { get; set; } = UserRole.Member;
    public bool IsActive { get; set; } = true;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset? LastLoginAt { get; set; }

    public Tenant Tenant { get; set; } = default!;
}

public sealed class ApplicationRole : IdentityRole<Guid>
{
    public string Description { get; set; } = default!;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}