using Microsoft.EntityFrameworkCore;
using OpenBusiness.Crm.Api.Data;
using OpenBusiness.Crm.Api.Security;
using OpenBusiness.Crm.Application.Activities;
using OpenBusiness.Crm.Domain;

namespace OpenBusiness.Crm.Api.Services.Activities;

public interface IActivityService
{
    Task<ActivityResponse> CreateAsync(CreateActivityRequest request, CancellationToken ct = default);
    Task<ActivityResponse?> GetAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<ActivityResponse>> ListAsync(Guid? customerId = null, Guid? leadId = null, int page = 1, int pageSize = 20, CancellationToken ct = default);
}

public sealed class ActivityService : IActivityService
{
    private readonly ApplicationDbContext _db;
    private readonly TenantContext _tenantContext;

    public ActivityService(ApplicationDbContext db, TenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<ActivityResponse> CreateAsync(CreateActivityRequest request, CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;
        var userId = _tenantContext.UserId;
        var activity = new Activity(tenantId, userId, request.Type, request.Subject, request.Description, request.CustomerId, request.LeadId, request.OccurredAt);
        _db.Activities.Add(activity);
        await _db.SaveChangesAsync(ct);
        return Map(activity);
    }

    public async Task<ActivityResponse?> GetAsync(Guid id, CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;
        var activity = await _db.Activities
            .FirstOrDefaultAsync(a => a.Id == id && a.TenantId == tenantId, ct);
        return activity is null ? null : Map(activity);
    }

    public async Task<IReadOnlyList<ActivityResponse>> ListAsync(Guid? customerId = null, Guid? leadId = null, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = _db.Activities.Where(a => a.TenantId == tenantId);

        if (customerId.HasValue)
            query = query.Where(a => a.CustomerId == customerId.Value);

        if (leadId.HasValue)
            query = query.Where(a => a.LeadId == leadId.Value);

        var activities = await query
            .OrderByDescending(a => a.OccurredAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return activities.Select(Map).ToList();
    }

    private static ActivityResponse Map(Activity a) => new(
        a.Id, a.CustomerId, a.LeadId, a.UserId, a.Type, a.Subject, a.Description, a.OccurredAt, a.CreatedAt);
}