using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using OpenBusiness.Crm.Api.Data;
using OpenBusiness.Crm.Domain;

namespace OpenBusiness.Crm.Api.Security;

public sealed class CurrentUser
{
    public Guid UserId { get; init; }
    public Guid TenantId { get; init; }
    public string Email { get; init; } = default!;
    public string FullName { get; init; } = default!;
    public string Role { get; init; } = default!;
    public bool IsAuthenticated { get; init; }

    public static CurrentUser Empty => new() { IsAuthenticated = false };
}

public interface ICurrentUserService
{
    CurrentUser GetCurrentUser();
    Task<CurrentUser> GetCurrentUserAsync(CancellationToken ct = default);
}

public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _db;
    private CurrentUser? _cachedUser;

    public CurrentUserService(
        IHttpContextAccessor httpContextAccessor,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext db)
    {
        _httpContextAccessor = httpContextAccessor;
        _userManager = userManager;
        _db = db;
    }

    public CurrentUser GetCurrentUser()
    {
        return GetCurrentUserAsync().GetAwaiter().GetResult();
    }

    public async Task<CurrentUser> GetCurrentUserAsync(CancellationToken ct = default)
    {
        if (_cachedUser != null)
            return _cachedUser;

        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext?.User?.Identity?.IsAuthenticated != true)
            return CurrentUser.Empty;

        var userIdClaim = httpContext.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(userIdClaim, out var userId))
            return CurrentUser.Empty;

        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null || !user.IsActive)
            return CurrentUser.Empty;

        var roles = await _userManager.GetRolesAsync(user);
        var tenant = await _db.Tenants.FindAsync(user.TenantId);

        _cachedUser = new CurrentUser
        {
            UserId = user.Id,
            TenantId = user.TenantId,
            Email = user.Email ?? string.Empty,
            FullName = user.FullName,
            Role = roles.FirstOrDefault() ?? "Member",
            IsAuthenticated = true
        };

        return _cachedUser;
    }
}

public static class CurrentUserExtensions
{
    public static CurrentUser GetCurrentUser(this HttpContext context)
    {
        var service = context.RequestServices.GetService(typeof(ICurrentUserService)) as ICurrentUserService;
        return service?.GetCurrentUser() ?? CurrentUser.Empty;
    }
}