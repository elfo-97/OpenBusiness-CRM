using Microsoft.EntityFrameworkCore;
using OpenBusiness.Crm.Api.Data;
using OpenBusiness.Crm.Api.Security;
using OpenBusiness.Crm.Application.Customers;
using OpenBusiness.Crm.Domain;

namespace OpenBusiness.Crm.Api.Services.Customers;

public interface ICustomerService
{
    Task<CustomerResponse> CreateAsync(CreateCustomerRequest request, CancellationToken ct = default);
    Task<CustomerResponse?> GetAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<CustomerResponse>> ListAsync(int page = 1, int pageSize = 20, CancellationToken ct = default);
    Task<CustomerResponse?> UpdateAsync(Guid id, UpdateCustomerRequest request, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}

public sealed class CustomerService : ICustomerService
{
    private readonly ApplicationDbContext _db;
    private readonly TenantContext _tenantContext;

    public CustomerService(ApplicationDbContext db, TenantContext tenantContext)
    {
        _db = db;
        _tenantContext = tenantContext;
    }

    public async Task<CustomerResponse> CreateAsync(CreateCustomerRequest request, CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;
        var customer = new Customer(tenantId, request.Name, request.Email, request.Phone, request.Document, request.Address);
        _db.Customers.Add(customer);
        await _db.SaveChangesAsync(ct);
        return Map(customer);
    }

    public async Task<CustomerResponse?> GetAsync(Guid id, CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;
        var customer = await _db.Customers
            .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenantId, ct);
        return customer is null ? null : Map(customer);
    }

    public async Task<IReadOnlyList<CustomerResponse>> ListAsync(int page = 1, int pageSize = 20, CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var customers = await _db.Customers
            .Where(c => c.TenantId == tenantId)
            .OrderByDescending(c => c.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
        return customers.Select(Map).ToList();
    }

    public async Task<CustomerResponse?> UpdateAsync(Guid id, UpdateCustomerRequest request, CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;
        var customer = await _db.Customers
            .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenantId, ct);
        if (customer is null) return null;
        customer.Update(request.Name, request.Email, request.Phone, request.Document, request.Address);
        await _db.SaveChangesAsync(ct);
        return Map(customer);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var tenantId = _tenantContext.TenantId;
        var customer = await _db.Customers
            .FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenantId, ct);
        if (customer is null) return false;
        customer.Deactivate();
        await _db.SaveChangesAsync(ct);
        return true;
    }

    private static CustomerResponse Map(Customer c) => new(
        c.Id, c.Name, c.Email, c.Phone, c.Document, c.Address, c.IsActive, c.CreatedAt, c.UpdatedAt);
}