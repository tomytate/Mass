# Mass Suite Security

## Authentication

### Dashboard Authentication

- ASP.NET Core Identity with cookie-based sessions
- Optional integration with Azure AD, Okta, or other OIDC providers
- Two-factor authentication (TOTP) supported

### Agent Authentication

- Pre-shared key (PSK) registration
- Certificate-based mutual TLS (production recommended)
- Token refresh via SignalR connection

### API Authentication

- Bearer JWT tokens
- Token expiration: 1 hour (configurable)
- Refresh tokens: 7 days

## Authorization

### Role-Based Access Control

| Role | Permissions |
|------|-------------|
| Admin | Full access, tenant management |
| Operator | Execute workflows, manage agents |
| Viewer | Read-only access |

### Tenant Isolation

- All data queries filtered by tenant ID
- Cross-tenant access prevented at infrastructure level
- Audit logging for tenant-switching operations

## Data Protection

### Secrets Management

- Secrets stored in environment variables or Azure Key Vault
- Never committed to source control
- Connection strings encrypted at rest

### Encryption

- HTTPS/TLS 1.3 for all network communication
- Data at rest encrypted via database-level encryption
- .NET Data Protection API for antiforgery tokens

## Input Validation

- Model validation via Data Annotations
- Path traversal prevention for file operations
- SQL injection prevention via parameterized queries (EF Core)

## Hardware Security

### Elevation Requirements

- USB write operations require administrator privileges
- Privilege check via `WindowsIdentity.GetCurrent().Owner`
- SafetyConfig.AllowRealWrites must be explicitly enabled

### Device Whitelisting

- Only removable devices allowed for burn operations
- System drives protected from accidental writes
- Drive signature verification pre-burn

## Logging and Auditing

- Structured logging with correlation IDs
- Sensitive data redacted from logs
- Audit trail for administrative actions

## Vulnerability Management

- Dependabot enabled for dependency updates
- Static analysis via Roslyn analyzers
- Regular security review cadence

## Incident Response

1. Isolate affected systems
2. Revoke compromised credentials
3. Review audit logs
4. Notify affected tenants (if applicable)
5. Post-incident review and remediation
