using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenBusiness.Crm.Api.Data;
using OpenBusiness.Crm.Api.Services.Auth;
using OpenBusiness.Crm.Application.Auth;
using OpenBusiness.Crm.Tests;

namespace OpenBusiness.Crm.Tests.Security;

public sealed class AuthServiceTests : IAsyncLifetime
{
    private readonly TestFixture _fixture;
    private IAuthService _authService = default!;
    private UserManager<ApplicationUser> _userManager = default!;

    public AuthServiceTests()
    {
        _fixture = new TestFixture();
    }

    public Task InitializeAsync() => _fixture.InitializeAsync();

    public async Task DisposeAsync() => await _fixture.DisposeAsync();

    [Fact]
    public async Task LoginAsync_ShouldReturnToken_ForValidCredentials()
    {
        _authService = _fixture.Services.GetRequiredService<IAuthService>();

        var result = await _authService.LoginAsync(new LoginRequest("test@example.com", "Test@123"));

        result.Should().NotBeNull();
        result!.AccessToken.Should().NotBeNullOrEmpty();
        result.TokenType.Should().Be("Bearer");
        result.ExpiresIn.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnNull_ForInvalidPassword()
    {
        _authService = _fixture.Services.GetRequiredService<IAuthService>();

        var result = await _authService.LoginAsync(new LoginRequest("test@example.com", "WrongPassword"));

        result.Should().BeNull();
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnNull_ForNonExistentUser()
    {
        _authService = _fixture.Services.GetRequiredService<IAuthService>();

        var result = await _authService.LoginAsync(new LoginRequest("nonexistent@test.com", "Test@123"));

        result.Should().BeNull();
    }

    [Fact]
    public async Task LoginAsync_ShouldReturnNull_ForInactiveUser()
    {
        _authService = _fixture.Services.GetRequiredService<IAuthService>();
        _userManager = _fixture.Services.GetRequiredService<UserManager<ApplicationUser>>();

        var user = await _userManager.FindByEmailAsync("test@example.com");
        user!.IsActive = false;
        await _userManager.UpdateAsync(user);

        var result = await _authService.LoginAsync(new LoginRequest("test@example.com", "Test@123"));

        result.Should().BeNull();
    }

    [Fact]
    public async Task RegisterAsync_ShouldCreateTenantAndOwner_AndReturnToken()
    {
        _authService = _fixture.Services.GetRequiredService<IAuthService>();
        _userManager = _fixture.Services.GetRequiredService<UserManager<ApplicationUser>>();

        var request = new RegisterRequest("New Tenant", "new-tenant", "newowner@test.com", "New Owner", "Secure@123");
        var result = await _authService.RegisterAsync(request);

        result.Should().NotBeNull();
        result!.AccessToken.Should().NotBeNullOrEmpty();

        // Verify tenant and user were created
        var tenant = await _fixture.DbContext.Tenants.FirstOrDefaultAsync(t => t.Slug == "new-tenant");
        tenant.Should().NotBeNull();
        tenant!.Name.Should().Be("New Tenant");

        var user = await _userManager.FindByEmailAsync("newowner@test.com");
        user.Should().NotBeNull();
        user!.TenantId.Should().Be(tenant!.Id);
        user.Role.Should().Be(UserRole.Owner);
    }

    [Fact]
    public async Task RegisterAsync_ShouldReturnNull_WhenSlugExists()
    {
        _authService = _fixture.Services.GetRequiredService<IAuthService>();

        var request1 = new RegisterRequest("Tenant 1", "same-slug", "user1@test.com", "User One", "Secure@123");
        await _authService.RegisterAsync(request1);

        var request2 = new RegisterRequest("Tenant 2", "same-slug", "user2@test.com", "User Two", "Secure@123");
        var result = await _authService.RegisterAsync(request2);

        result.Should().BeNull();
    }

    [Fact]
    public async Task RegisterAsync_ShouldReturnNull_WhenEmailExists()
    {
        _authService = _fixture.Services.GetRequiredService<IAuthService>();

        var request1 = new RegisterRequest("Tenant 1", "tenant-1", "existing@test.com", "User One", "Secure@123");
        await _authService.RegisterAsync(request1);

        var request2 = new RegisterRequest("Tenant 2", "tenant-2", "existing@test.com", "User Two", "Secure@123");
        var result = await _authService.RegisterAsync(request2);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetCurrentUserAsync_ShouldReturnUser_WhenExists()
    {
        _authService = _fixture.Services.GetRequiredService<IAuthService>();

        var result = await _authService.GetCurrentUserAsync(_fixture.UserId);

        result.Should().NotBeNull();
        result!.Id.Should().Be(_fixture.UserId);
        result.TenantId.Should().Be(_fixture.TenantId);
        result.Email.Should().Be("test@example.com");
        result.FullName.Should().Be("Test User");
        result.Role.Should().Be("Owner");
    }

    [Fact]
    public async Task GetCurrentUserAsync_ShouldReturnNull_WhenNotExists()
    {
        _authService = _fixture.Services.GetRequiredService<IAuthService>();

        var result = await _authService.GetCurrentUserAsync(Guid.NewGuid());

        result.Should().BeNull();
    }
}