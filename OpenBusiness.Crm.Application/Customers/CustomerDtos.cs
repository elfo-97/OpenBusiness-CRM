namespace OpenBusiness.Crm.Application.Customers;

public sealed record CreateCustomerRequest(
    string Name,
    string? Email = null,
    string? Phone = null,
    string? Document = null,
    string? Address = null
);

public sealed record UpdateCustomerRequest(
    string Name,
    string? Email = null,
    string? Phone = null,
    string? Document = null,
    string? Address = null
);

public sealed record CustomerResponse(
    Guid Id,
    string Name,
    string? Email,
    string? Phone,
    string? Document,
    string? Address,
    bool IsActive,
    DateTimeOffset CreatedAt,
    DateTimeOffset? UpdatedAt
);