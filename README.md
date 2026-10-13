# OpenBusiness CRM API

A production-minded .NET 10 CRM API with JWT authentication, multi-tenant isolation, role-based authorization, and PostgreSQL persistence.

## Features

- **Authentication**: JWT Bearer with ASP.NET Core Identity
- **Authorization**: Role-based (Owner, Admin, Member) with policy-based access control
- **Multi-tenant isolation**: Tenant boundary enforced on every operation
- **Rate limiting**: IP-based rate limiting for auth endpoints
- **Database**: PostgreSQL with EF Core migrations
- **Testing**: 85 tests covering auth, roles, tenant isolation, CRUD operations

## Quick Start

### Prerequisites

- .NET 10 SDK
- PostgreSQL 14+ (or Docker)

### Configuration

1. Copy `OpenBusiness.Crm.Api/appsettings.json` to `appsettings.Development.json`
2. Update the connection string and JWT settings:

```json
{
  "ConnectionStrings": {
    "Default": "Host=localhost;Port=5432;Database=openbusiness_crm;Username=postgres;Password=your_password"
  },
  "Jwt": {
    "SecretKey": "your-256-bit-secret-key-here-use-user-secrets-in-dev",
    "Issuer": "OpenBusiness.Crm",
    "Audience": "OpenBusiness.Crm.Api",
    "ExpiryMinutes": 60
  }
}
```

### Development Secrets

```bash
cd OpenBusiness.Crm.Api
dotnet user-secrets init
dotnet user-secrets set "Jwt:SecretKey" "your-256-bit-secret-key-here"
dotnet user-secrets set "ConnectionStrings:Default" "Host=localhost;Port=5432;Database=openbusiness_crm;Username=postgres;Password=your_password"
```

### Database Setup

```bash
# Apply migrations to PostgreSQL
cd OpenBusiness.Crm.Api
dotnet ef database update

# Or with connection string
dotnet ef database update --connection "Host=localhost;Port=5432;Database=openbusiness_crm;Username=postgres;Password=your_password"
```

### Run

```bash
# From solution root
dotnet run --project OpenBusiness.Crm.Api
```

API available at `https://localhost:5001` (or `http://localhost:5000`)

## API Endpoints

### Authentication

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| POST | `/api/auth/login` | Anonymous | Login with email/password |
| POST | `/api/auth/register` | Anonymous | Create tenant + owner |
| GET | `/api/auth/me` | Required | Current user info |

### Customers

| Method | Endpoint | Policy | Description |
|--------|----------|--------|-------------|
| POST | `/api/customers` | MemberOrAbove | Create customer |
| GET | `/api/customers/{id}` | MemberOrAbove | Get customer |
| GET | `/api/customers` | MemberOrAbove | List customers (paged) |
| PUT | `/api/customers/{id}` | MemberOrAbove | Update customer |
| DELETE | `/api/customers/{id}` | MemberOrAbove | Deactivate customer |

### Leads

| Method | Endpoint | Policy | Description |
|--------|----------|--------|-------------|
| POST | `/api/leads` | MemberOrAbove | Create lead |
| GET | `/api/leads/{id}` | MemberOrAbove | Get lead |
| GET | `/api/leads` | MemberOrAbove | List leads (paged) |
| PUT | `/api/leads/{id}` | MemberOrAbove | Update lead |
| PATCH | `/api/leads/{id}/stage` | MemberOrAbove | Change stage |
| DELETE | `/api/leads/{id}` | MemberOrAbove | Deactivate lead |

### Contacts

| Method | Endpoint | Policy | Description |
|--------|----------|--------|-------------|
| POST | `/api/customers/{customerId}/contacts` | MemberOrAbove | Create contact |
| GET | `/api/customers/{customerId}/contacts/{id}` | MemberOrAbove | Get contact |
| GET | `/api/customers/{customerId}/contacts` | MemberOrAbove | List contacts (paged) |
| PUT | `/api/customers/{customerId}/contacts/{id}` | MemberOrAbove | Update contact |
| DELETE | `/api/customers/{customerId}/contacts/{id}` | MemberOrAbove | Deactivate contact |

### Activities

| Method | Endpoint | Policy | Description |
|--------|----------|--------|-------------|
| POST | `/api/activities` | MemberOrAbove | Create activity |
| GET | `/api/activities/{id}` | MemberOrAbove | Get activity |
| GET | `/api/activities` | MemberOrAbove | List activities (paged, filterable) |

### Health

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| GET | `/health` | Anonymous | Basic liveness |

## Authorization Matrix

| Endpoint | Owner | Admin | Member |
|----------|-------|-------|--------|
| `/api/auth/login` | ✓ | ✓ | ✓ (public) |
| `/api/auth/register` | ✓ | ✓ | ✓ (public) |
| `/api/auth/me` | ✓ | ✓ | ✓ |
| `POST /api/customers` | ✓ | ✓ | ✓ |
| `GET /api/customers` | ✓ | ✓ | ✓ |
| `PUT /api/customers/{id}` | ✓ | ✓ | ✓ |
| `DELETE /api/customers/{id}` | ✓ | ✓ | ✓ |
| `POST /api/leads` | ✓ | ✓ | ✓ |
| `PATCH /api/leads/{id}/stage` | ✓ | ✓ | ✓ |
| `POST /api/contacts` | ✓ | ✓ | ✓ |
| `POST /api/activities` | ✓ | ✓ | ✓ |
| Tenant admin* | ✓ | ✗ | ✗ |
| Role assignment* | ✓ | ✗ | ✗ |

*Not implemented as endpoints; role changes via direct DB or future admin API

## Roles

- **Owner**: Full tenant control, can manage users/roles
- **Admin**: CRM data administration, user management (except ownership)
- **Member**: Standard CRM operations

## Rate Limiting

| Endpoint | Limit | Window |
|----------|-------|--------|
| Global | 100 req/min per IP | 1 minute |
| `/api/auth/login` | 5 req/min per IP | 1 minute |
| `/api/auth/register` | 5 req/min per IP | 1 minute |

Returns `429 Too Many Requests` when exceeded.

## Testing

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test --collect:"XPlat Code Coverage"
```

### Test Coverage (85 tests)

- **Auth**: Login success/failure, registration, token validation
- **JWT**: Expiry, tampering, issuer/audience, claims
- **Roles**: Owner/Admin/Member token claims
- **Tenant Isolation**: Cross-tenant access denied for all resources
- **CRUD**: Create/Read/Update/Delete for all entities
- **Domain**: Entity validation, enums, business rules

## Security

- Passwords: 8+ chars, digit, upper, lower, special char
- Lockout: 5 failed attempts → 15 min lockout
- JWT: HS256, 60 min expiry, validate issuer/audience/lifetime/signature
- Tokens: No refresh tokens (access tokens only, immediate revocation not supported)
- Secrets: Never committed; use User Secrets or env vars
- CORS: Restrict in production (`AllowedHosts`)

## Production Deployment

### Environment Variables

```
ConnectionStrings__Default=Host=...;Database=...;Username=...;Password=...
Jwt__SecretKey=...
Jwt__Issuer=OpenBusiness.Crm
Jwt__Audience=OpenBusiness.Crm.Api
Jwt__ExpiryMinutes=60
ASPNETCORE_ENVIRONMENT=Production
ASPNETCORE_URLS=http://+:8080
```

### Docker (example)

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY OpenBusiness.Crm.Api/bin/Release/net10.0/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "OpenBusiness.Crm.Api.dll"]
```

### Migration Commands

```bash
# Create migration
dotnet ef migrations add MigrationName --project OpenBusiness.Crm.Api

# Apply to database
dotnet ef database update --project OpenBusiness.Crm.Api

# Remove last migration (dev only)
dotnet ef migrations remove --project OpenBusiness.Crm.Api
```

## Project Structure

```
OpenBusiness.Crm.sln
├── OpenBusiness.Crm.Domain/      # Entities, enums
├── OpenBusiness.Crm.Application/ # DTOs (request/response)
├── OpenBusiness.Crm.Api/         # Controllers, services, EF Core, auth
└── OpenBusiness.Crm.Tests/       # Unit + integration tests
```

## License

MIT