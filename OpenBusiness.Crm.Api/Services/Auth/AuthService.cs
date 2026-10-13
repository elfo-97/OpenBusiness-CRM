using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OpenBusiness.Crm.Api.Data;
using OpenBusiness.Crm.Api.Security;
using OpenBusiness.Crm.Application.Auth;
using OpenBusiness.Crm.Domain;

namespace OpenBusiness.Crm.Api.Services.Auth;

public interface IAuthService
{
    Task<TokenResponse?> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<TokenResponse?> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task<AuthUserResponse?> GetCurrentUserAsync(Guid userId, CancellationToken ct = default);
}

public sealed class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly ApplicationDbContext _db;
    private readonly JwtTokenGenerator _jwtGenerator;

    public AuthService(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ApplicationDbContext db,
        JwtTokenGenerator jwtGenerator)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _db = db;
        _jwtGenerator = jwtGenerator;
    }

    public async Task<TokenResponse?> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user is null || !user.IsActive)
            return null;

        var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: false);
        if (!result.Succeeded)
            return null;

        user.LastLoginAt = DateTimeOffset.UtcNow;
        await _userManager.UpdateAsync(user);

        var roles = await _userManager.GetRolesAsync(user);
        var tenant = await _db.Tenants.FindAsync(user.TenantId);
        
        return _jwtGenerator.GenerateToken(user, tenant?.Name ?? string.Empty, roles.FirstOrDefault() ?? "Member");
    }

    public async Task<TokenResponse?> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        var slugExists = await _db.Tenants.AnyAsync(t => t.Slug == request.TenantSlug, ct);
        if (slugExists)
            return null;

        var emailExists = await _userManager.FindByEmailAsync(request.Email);
        if (emailExists != null)
            return null;

        // Use a strategy that works with both in-memory and real databases
        var strategy = _db.Database.CreateExecutionStrategy();
        return await strategy.ExecuteAsync(async () =>
        {
            // For in-memory, we don't use transactions
            var isInMemory = _db.Database.ProviderName?.Contains("InMemory") == true;
            
            using var transaction = isInMemory ? null : await _db.Database.BeginTransactionAsync(ct);
            try
            {
                var tenant = new Tenant(request.TenantName, request.TenantSlug);
                _db.Tenants.Add(tenant);
                await _db.SaveChangesAsync(ct);

                var user = new ApplicationUser
                {
                    UserName = request.Email,
                    Email = request.Email,
                    FullName = request.FullName,
                    TenantId = tenant.Id,
                    Role = UserRole.Owner,
                    IsActive = true,
                    EmailConfirmed = true
                };

                var createResult = await _userManager.CreateAsync(user, request.Password);
                if (!createResult.Succeeded)
                {
                    if (transaction != null)
                        await transaction.RollbackAsync(ct);
                    return null;
                }

                await _userManager.AddToRoleAsync(user, "Owner");
                if (transaction != null)
                    await transaction.CommitAsync(ct);

                return _jwtGenerator.GenerateToken(user, tenant.Name, "Owner");
            }
            catch
            {
                if (transaction != null)
                    await transaction.RollbackAsync(ct);
                throw;
            }
        });
    }

    public async Task<AuthUserResponse?> GetCurrentUserAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _userManager.FindByIdAsync(userId.ToString());
        if (user is null || !user.IsActive)
            return null;

        var roles = await _userManager.GetRolesAsync(user);
        var tenant = await _db.Tenants.FindAsync(user.TenantId);

        return new AuthUserResponse(
            user.Id,
            user.TenantId,
            tenant?.Name ?? string.Empty,
            user.Email ?? string.Empty,
            user.FullName,
            roles.FirstOrDefault() ?? "Member"
        );
    }
}

public sealed class JwtTokenGenerator
{
    private readonly JwtSettings _settings;

    public JwtTokenGenerator(JwtSettings settings) => _settings = settings;

    public TokenResponse GenerateToken(ApplicationUser user, string tenantName, string role)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(ClaimTypes.Name, user.FullName),
            new("tenant_id", user.TenantId.ToString()),
            new("tenant_name", tenantName),
            new(ClaimTypes.Role, role)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SecretKey));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddMinutes(_settings.ExpiryMinutes);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: expires,
            signingCredentials: creds
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        return new TokenResponse(
            tokenString,
            "Bearer",
            _settings.ExpiryMinutes * 60,
            string.Empty // Refresh token not implemented
        );
    }
}

public sealed class JwtSettings
{
    public string SecretKey { get; set; } = default!;
    public string Issuer { get; set; } = default!;
    public string Audience { get; set; } = default!;
    public int ExpiryMinutes { get; set; } = 60;
}