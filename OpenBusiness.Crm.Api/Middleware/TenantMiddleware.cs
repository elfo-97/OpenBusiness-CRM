using OpenBusiness.Crm.Api.Security;

namespace OpenBusiness.Crm.Api.Middleware;

public sealed class TenantMiddleware
{
    private readonly RequestDelegate _next;

    public TenantMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, ITenantResolver tenantResolver)
    {
        var tenantContext = await tenantResolver.ResolveAsync(context);
        context.Items["TenantContext"] = tenantContext;
        await _next(context);
    }
}

public interface ITenantResolver
{
    Task<TenantContext> ResolveAsync(HttpContext context);
}

public sealed class HeaderTenantResolver : ITenantResolver
{
    private const string TenantHeader = "X-Tenant-Id";
    private const string UserHeader = "X-User-Id";
    private const string EmailHeader = "X-User-Email";
    private const string NameHeader = "X-User-Name";

    public Task<TenantContext> ResolveAsync(HttpContext context)
    {
        if (!context.Request.Headers.TryGetValue(TenantHeader, out var tenantIdStr) ||
            !Guid.TryParse(tenantIdStr.FirstOrDefault(), out var tenantId))
        {
            return Task.FromResult(TenantContext.Empty);
        }

        if (!context.Request.Headers.TryGetValue(UserHeader, out var userIdStr) ||
            !Guid.TryParse(userIdStr.FirstOrDefault(), out var userId))
        {
            return Task.FromResult(TenantContext.Empty);
        }

        var email = context.Request.Headers[EmailHeader].FirstOrDefault() ?? string.Empty;
        var name = context.Request.Headers[NameHeader].FirstOrDefault() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(name))
        {
            return Task.FromResult(TenantContext.Empty);
        }

        return Task.FromResult(TenantContext.Create(tenantId, userId, email, name));
    }
}

public static class TenantContextExtensions
{
    public static TenantContext GetTenantContext(this HttpContext context)
        => context.Items.TryGetValue("TenantContext", out var value) && value is TenantContext tc
            ? tc
            : TenantContext.Empty;

    public static bool TryGetTenantContext(this HttpContext context, out TenantContext tenantContext)
    {
        tenantContext = context.GetTenantContext();
        return tenantContext.IsAuthenticated;
    }
}