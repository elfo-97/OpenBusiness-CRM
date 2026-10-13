using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenBusiness.Crm.Api.Data;
using OpenBusiness.Crm.Api.Middleware;
using OpenBusiness.Crm.Api.Security;
using OpenBusiness.Crm.Api.Services.Customers;
using OpenBusiness.Crm.Api.Services.Leads;
using OpenBusiness.Crm.Api.Services.Activities;
using OpenBusiness.Crm.Api.Services.Contacts;

namespace OpenBusiness.Crm.Tests;

public sealed class TestFixture : IAsyncLifetime
{
    public IServiceProvider Services { get; private set; } = default!;
    public ApplicationDbContext DbContext { get; private set; } = default!;
    public TenantContext TenantContext { get; private set; } = default!;
    public Guid TenantId { get; } = Guid.NewGuid();
    public Guid UserId { get; } = Guid.NewGuid();

    public async Task InitializeAsync()
    {
        var services = new ServiceCollection();

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}"));

        services.AddScoped<ITenantResolver, TestTenantResolver>();
        services.AddScoped(_ => TenantContext);
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<ILeadService, LeadService>();
        services.AddScoped<IActivityService, ActivityService>();
        services.AddScoped<IContactService, ContactService>();

        services.AddHttpContextAccessor();

        Services = services.BuildServiceProvider();

        DbContext = Services.GetRequiredService<ApplicationDbContext>();
        await DbContext.Database.EnsureCreatedAsync();

        TenantContext = TenantContext.Create(TenantId, UserId, "test@example.com", "Test User");
    }

    public async Task DisposeAsync()
    {
        if (DbContext != null)
        {
            await DbContext.DisposeAsync();
        }
        if (Services is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }

    public async Task ResetAsync()
    {
        if (DbContext != null)
        {
            await DbContext.Database.EnsureDeletedAsync();
            await DbContext.Database.EnsureCreatedAsync();
        }
    }

    private sealed class TestTenantResolver : ITenantResolver
    {
        private readonly TenantContext _context;

        public TestTenantResolver(TenantContext context)
        {
            _context = context;
        }

        public Task<TenantContext> ResolveAsync(HttpContext context)
            => Task.FromResult(_context);
    }
}