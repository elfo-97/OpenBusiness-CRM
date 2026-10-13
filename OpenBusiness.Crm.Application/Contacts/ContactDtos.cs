namespace OpenBusiness.Crm.Application.Contacts;

public sealed record CreateContactRequest(
    Guid CustomerId,
    string Name,
    string? Email = null,
    string? Phone = null,
    string? Role = null,
    bool IsPrimary = false
);

public sealed record UpdateContactRequest(
    string Name,
    string? Email = null,
    string? Phone = null,
    string? Role = null,
    bool IsPrimary = false
);

public sealed record ContactResponse(
    Guid Id,
    Guid CustomerId,
    string Name,
    string? Email,
    string? Phone,
    string? Role,
    bool IsPrimary,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt
);