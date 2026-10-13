using Microsoft.EntityFrameworkCore;
using OpenBusiness.Crm.Api.Data;
using OpenBusiness.Crm.Api.Security;
using OpenBusiness.Crm.Application.Leads;
using OpenBusiness.Crm.Domain;

namespace OpenBusiness.Crm.Api.Services.Leads;

public interface ILeadService
{
    Task<LeadResponse> CreateAsync(CreateLeadRequest request, CancellationToken ct = default);
    Task<LeadResponse?> GetAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<LeadResponse>> ListAsync(int page = 1, int pageSize = 20, CancellationToken ct = default);
    Task<LeadResponse?> UpdateAsync(Guid id, UpdateLeadRequest request, CancellationToken ct = default);
    Task<LeadResponse?> ChangeStageAsync(Guid id, ChangeLeadStageRequest request, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}

public sealed class LeadService : ILeadService
{
    private readonly ApplicationDbContext _db;
    private readonly TenantContext _tenantContext;

    public LeadService(ApplicationDbContext db, TenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<LeadResponse> CreateAsync(CreateLeadRequest request, CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;
        var lead = new Lead(tenantId, request.Title, request.Description, request.CustomerId, request.EstimatedValue, request.ExpectedCloseDate, request.AssignedToUserId);
        _db.Leads.Add(lead);
        await _db.SaveChangesAsync(ct);
        return Map(lead);
    }

    public async Task<LeadResponse?> GetAsync(Guid id, CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;
        var lead = await _db.Leads
            .FirstOrDefaultAsync(l => l.Id == id && l.TenantId == tenantId, ct);
        return lead is null ? null : Map(lead);
    }

    public async Task<IReadOnlyList<LeadResponse>> ListAsync(int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var leads = await _db.Leads
            .Where(l => l.TenantId == tenantId)
            .OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
        return leads.Select(Map).ToList();
    }

    public async Task<LeadResponse?> UpdateAsync(Guid id, UpdateLeadRequest request, CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;
        var lead = await _db.Leads
            .FirstOrDefaultAsync(l => l.Id == id && l.TenantId == tenantId, ct);
        if (lead is null) return null;
        lead.Update(request.Title, request.Description, request.CustomerId, request.EstimatedValue, request.ExpectedCloseDate, request.AssignedToUserId);
        await _db.SaveChangesAsync(ct);
        return Map(lead);
    }

    public async Task<LeadResponse?> ChangeStageAsync(Guid id, ChangeLeadStageRequest request, CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;
        var lead = await _db.Leads
            .FirstOrDefaultAsync(l => l.Id == id && l.TenantId == tenantId, ct);
        if (lead is null) return null;
        lead.ChangeStage(request.Stage);
        await _db.SaveChangesAsync(ct);
        return Map(lead);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;
        var lead = await _db.Leads
            .FirstOrDefaultAsync(l => l.Id == id && l.TenantId == tenantId, ct);
        if (lead is null) return false;
        lead.Deactivate();
        await _db.SaveChangesAsync(ct);
        return true;
    }

    private static LeadResponse Map(Lead l) => new(
        l.Id, l.CustomerId, l.Title, l.Description, l.Stage, l.EstimatedValue, l.ExpectedCloseDate, l.AssignedToUserId, l.CreatedAt, l.UpdatedAt, l.ClosedAt);
}