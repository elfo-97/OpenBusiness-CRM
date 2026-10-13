using Microsoft.EntityFrameworkCore;
using OpenBusiness.Crm.Api.Data;
using OpenBusiness.Crm.Api.Security;
using OpenBusiness.Crm.Application.Contacts;
using OpenBusiness.Crm.Domain;

namespace OpenBusiness.Crm.Api.Services.Contacts;

public interface IContactService
{
    Task<ContactResponse> CreateAsync(CreateContactRequest request, CancellationToken ct = default);
    Task<ContactResponse?> GetAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<ContactResponse>> ListAsync(Guid customerId, int page = 1, int pageSize = 20, CancellationToken ct = default);
    Task<ContactResponse?> UpdateAsync(Guid id, UpdateContactRequest request, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}

public sealed class ContactService : IContactService
{
    private readonly ApplicationDbContext _db;
    private readonly TenantContext _tenantContext;

    public ContactService(ApplicationDbContext db, TenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<ContactResponse> CreateAsync(CreateContactRequest request, CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;
        var contact = new Contact(tenantId, request.CustomerId, request.Name, request.Email, request.Phone, request.Role, request.IsPrimary);
        _db.Contacts.Add(contact);
        await _db.SaveChangesAsync(ct);
        return Map(contact);
    }

    public async Task<ContactResponse?> GetAsync(Guid id, CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;
        var contact = await _db.Contacts
            .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenantId, ct);
        return contact is null ? null : Map(contact);
    }

    public async Task<IReadOnlyList<ContactResponse>> ListAsync(Guid customerId, int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var contacts = await _db.Contacts
            .Where(c => c.TenantId == tenantId && c.CustomerId == customerId)
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
        return contacts.Select(Map).ToList();
    }

    public async Task<ContactResponse?> UpdateAsync(Guid id, UpdateContactRequest request, CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;
        var contact = await _db.Contacts
            .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenantId, ct);
        if (contact is null) return null;
        contact.Update(request.Name, request.Email, request.Phone, request.Role, request.IsPrimary);
        await _db.SaveChangesAsync(ct);
        return Map(contact);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;
        var contact = await _db.Contacts
            .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenantId, ct);
        if (contact is null) return false;
        contact.Deactivate();
        await _db.SaveChangesAsync(ct);
        return true;
    }

    private static ContactResponse Map(Contact c) => new(
        c.Id, c.CustomerId, c.Name, c.Email, c.Phone, c.Role, c.IsPrimary, c.IsActive, c.CreatedAt, c.UpdatedAt);
}