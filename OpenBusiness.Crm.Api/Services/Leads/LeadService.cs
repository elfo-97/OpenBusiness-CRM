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
    private readonly ICurrentUserService _currentUser;

    public LeadService(ApplicationDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    private Guid TenantId => _currentUser.GetCurrentUser().TenantId;

    public async Task<LeadResponse> CreateAsync(CreateLeadRequest request, CancellationToken ct = default)
    {
        var lead = new Lead(TenantId, request.Title, request.Description, request.CustomerId, request.EstimatedValue, request.ExpectedCloseDate, request.AssignedToUserId);
        _db.Leads.Add(lead);
        await _db.SaveChangesAsync(ct);
        return Map(lead);
    }

    public async Task<LeadResponse?> GetAsync(Guid id, CancellationToken ct = default)
    {
        var lead = await _db.Leads
            .FirstOrDefaultAsync(l => l.Id == id && l.TenantId == TenantId, ct);
        return lead is null ? null : Map(lead);
    }

    public async Task<IReadOnlyList<LeadResponse>> ListAsync(int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var leads = await _db.Leads
            .Where(l => l.TenantId == TenantId)
            .OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
        return leads.Select(Map).ToList();
    }

    public async Task<LeadResponse?> UpdateAsync(Guid id, UpdateLeadRequest request, CancellationToken ct = default)
    {
        var lead = await _db.Leads
            .FirstOrDefaultAsync(l => l.Id == id && l.TenantId == TenantId, ct);
        if (lead is null) return null;
        lead.Update(request.Title, request.Description, request.CustomerId, request.EstimatedValue, request.ExpectedCloseDate, request.AssignedToUserId);
        await _db.SaveChangesAsync(ct);
        return Map(lead);
    }

    public async Task<LeadResponse?> ChangeStageAsync(Guid id, ChangeLeadStageRequest request, CancellationToken ct = default)
    {
        var lead = await _db.Leads
            .FirstOrDefaultAsync(l => l.Id == id && l.TenantId == TenantId, ct);
        if (lead is null) return null;
        lead.ChangeStage(request.Stage);
        await _db.SaveChangesAsync(ct);
        return Map(lead);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var lead = await _db.Leads
            .FirstOrDefaultAsync(l => l.Id == id && l.TenantId == TenantId, ct);
        if (lead is null) return false;
        lead.Deactivate();
        await _db.SaveChangesAsync(ct);
        return true;
    }

    private static LeadResponse Map(Lead l) => new(
        l.Id, l.CustomerId, l.Title, l.Description, l.Stage, l.EstimatedValue, l.ExpectedCloseDate, l.AssignedToUserId, l.CreatedAt, l.UpdatedAt, l.ClosedAt);
}