namespace OpenBusiness.Crm.Application.Auth;

public sealed record LoginRequest(
    string Email,
    string Password
);

public sealed record RegisterRequest(
    string TenantName,
    string TenantSlug,
    string Email,
    string FullName,
    string Password
);

public sealed record TokenResponse(
    string AccessToken,
    string TokenType,
    int ExpiresIn,
    string RefreshToken
);

public sealed record AuthUserResponse(
    Guid Id,
    Guid TenantId,
    string TenantName,
    string Email,
    string FullName,
    string Role
);