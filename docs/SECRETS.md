# Mass Suite - Secrets Management Guide

## Overview

This document describes the secure configuration of secrets for Mass Suite production deployments.

---

## Required Secrets

| Secret | Environment Variable | Description | Requirements |
|--------|---------------------|-------------|--------------|
| JWT Secret Key | `MASS_JWTSETTINGS__SECRETKEY` | Signs authentication tokens | **32+ characters**, random |
| Stripe Secret Key | `MASS_STRIPE__SECRETKEY` | Stripe API authentication | From Stripe Dashboard |
| Stripe Webhook Secret | `MASS_STRIPE__WEBHOOKSECRET` | Validates webhook signatures | From Stripe Dashboard |

---

## Configuration Methods

### Method 1: Environment Variables (Recommended for Production)

```bash
# Linux/macOS
export MASS_JWTSETTINGS__SECRETKEY="your-32-character-minimum-secret-key-here"
export MASS_STRIPE__SECRETKEY="sk_live_..."
export MASS_STRIPE__WEBHOOKSECRET="whsec_..."

# Windows (PowerShell)
$env:MASS_JWTSETTINGS__SECRETKEY = "your-32-character-minimum-secret-key-here"
```

### Method 2: User Secrets (Development Only)

```bash
dotnet user-secrets set "JwtSettings:SecretKey" "dev-only-secret-key-32-chars-min"
```

### Method 3: Azure Key Vault (Enterprise)

```csharp
builder.Configuration.AddAzureKeyVault(
    new Uri("https://your-vault.vault.azure.net/"),
    new DefaultAzureCredential());
```

---

## Generating Secure Keys

### JWT Secret Key (32+ characters)

```powershell
# PowerShell
[Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(48))
```

```bash
# Linux/macOS
openssl rand -base64 48
```

---

## Security Best Practices

1. **Never commit secrets** to source control
2. **Rotate secrets** regularly (every 90 days recommended)
3. **Use different secrets** for each environment (dev/staging/prod)
4. **Monitor access** via audit logs in `%APPDATA%/MassSuite/Audit/`
5. **Validate at startup** - application blocks if required secrets are missing

---

## Startup Validation

The application validates secrets at startup in production mode:

- **JwtSettings:SecretKey** - Must be 32+ characters
- **Stripe:SecretKey** - Must be present
- **Stripe:WebhookSecret** - Must be present

If validation fails, the application will not start and logs a `CRITICAL` error.

---

## Credential Storage

User credentials are stored encrypted using:
- **Windows**: DPAPI (Windows Data Protection API)
- **Linux/macOS**: AES-256-GCM with machine-derived key

Location: `%APPDATA%/MassSuite/credentials.dat`
