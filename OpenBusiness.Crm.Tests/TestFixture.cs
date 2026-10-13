using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenBusiness.Crm.Api.Data;
using OpenBusiness.Crm.Api.Security;
using OpenBusiness.Crm.Api.Services.Auth;
using OpenBusiness.Crm.Api.Services.Customers;
using OpenBusiness.Crm.Api.Services.Leads;
using OpenBusiness.Crm.Api.Services.Activities;
using OpenBusiness.Crm.Api.Services.Contacts;
using OpenBusiness.Crm.Domain;

namespace OpenBusiness.Crm.Tests;

public sealed class TestFixture : IAsyncLifetime
{
    public IServiceProvider Services { get; private set; } = default!;
    public ApplicationDbContext DbContext { get; private set; } = default!;
    public CurrentUser CurrentUser { get; private set; } = default!;
    public Guid TenantId { get; } = Guid.NewGuid();
    public Guid UserId { get; } = Guid.NewGuid();

    public async Task InitializeAsync()
    {
        var services = new ServiceCollection();

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}"));

        services.AddLogging();

        services.AddIdentity<ApplicationUser, ApplicationRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        // Register JwtSettings for tests
        services.AddSingleton(new JwtSettings
        {
            SecretKey = "TestSecretKeyThatIsLongEnoughForHS256Algorithm12345",
            Issuer = "OpenBusiness.Crm",
            Audience = "OpenBusiness.Crm.Api",
            ExpiryMinutes = 60
        });
        services.AddScoped<JwtTokenGenerator>();

        // Register TestFixture as singleton so TestCurrentUserService can resolve it
        services.AddSingleton(this);

        services.AddScoped<ICurrentUserService, TestCurrentUserService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<ILeadService, LeadService>();
        services.AddScoped<IActivityService, ActivityService>();
        services.AddScoped<IContactService, ContactService>();

        services.AddHttpContextAccessor();

        Services = services.BuildServiceProvider();

        DbContext = Services.GetRequiredService<ApplicationDbContext>();
        await DbContext.Database.EnsureCreatedAsync();

        // Create test tenant and user in database
        var tenant = new Tenant("Test Tenant", "test-tenant");
        tenant.GetType().GetProperty("Id")?.SetValue(tenant, TenantId);
        DbContext.Tenants.Add(tenant);

        var user = new ApplicationUser
        {
            Id = UserId,
            UserName = "test@example.com",
            Email = "test@example.com",
            FullName = "Test User",
            TenantId = TenantId,
            Role = UserRole.Owner,
            IsActive = true,
            EmailConfirmed = true
        };

        var userManager = Services.GetRequiredService<UserManager<ApplicationUser>>();
        await userManager.CreateAsync(user, "Test@123");
        await userManager.AddToRoleAsync(user, "Owner");

        CurrentUser = new CurrentUser
        {
            UserId = UserId,
            TenantId = TenantId,
            Email = "test@example.com",
            FullName = "Test User",
            Role = "Owner",
            IsAuthenticated = true
        };
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

    private sealed class TestCurrentUserService : ICurrentUserService
    {
        private readonly TestFixture _fixture;

        public TestCurrentUserService(TestFixture fixture)
        {
            _fixture = fixture;
        }

        public CurrentUser GetCurrentUser()
            => _fixture.CurrentUser;

        public Task<CurrentUser> GetCurrentUserAsync(CancellationToken ct = default)
            => Task.FromResult(_fixture.CurrentUser);
    }
}