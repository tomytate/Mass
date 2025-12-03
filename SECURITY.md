# Security Policy

## Version 1.0.0

This document outlines the security measures implemented in Mass Suite and best practices for secure deployment.

## 🔒 Authentication & Authorization

### JWT Configuration
Mass Suite uses JWT (JSON Web Tokens) for API authentication. **Never commit JWT secrets to source control.**

**Development:**
```bash
# Auto-generated 32+ character key (logged as warning)
dotnet run
```

**Production:**
```bash
# Set via environment variable (REQUIRED)
export MASS_JWTSETTINGS__SECRETKEY="your-secure-256-bit-key-here"
```

Generate a secure key:
```bash
openssl rand -base64 32
```

### Token Expiry
Default: 60 minutes. Configure via:
```bash
export MASS_JWTSETTINGS__EXPIRYMINUTES=60
```

## 🌐 CORS (Cross-Origin Resource Sharing)

### Development Mode
- **Allows all origins** for ease of development
- Automatically enabled when `ASPNETCORE_ENVIRONMENT=Development`

### Production Mode
- **Strict origin whitelist required**
- Configure allowed origins:

```json
{
  "Cors": {
    "AllowedOrigins": [
      "https://your-production-domain.com",
      "https://admin.your-domain.com"
    ]
  }
}
```

Or via environment variables:
```bash
export MASS_CORS__ALLOWEDORIGINS__0="https://your-domain.com"
export MASS_CORS__ALLOWEDORIGINS__1="https://admin.your-domain.com"
```

## 🛡️ Path Traversal Protection

Boot file endpoints implement **defense-in-depth** against directory traversal:

1. **Filename validation**: Only `[a-zA-Z0-9_\-\.]` allowed
2. **Explicit `..` rejection**: Any path containing `..` is blocked
3. **Path normalization check**: Normalized path must start with `pxeRoot`

Example protected endpoint:
```csharp
// ❌ Blocked: path.Contains("..")
// ❌ Blocked: !SafeFilenameRegex().IsMatch(fileName)
// ❌ Blocked: !fullPath.StartsWith(_pxeRoot)
```

## 🔑 Stripe Integration

Never commit Stripe keys. Use environment variables:

```bash
export MASS_STRIPE__SECRETKEY="sk_live_..."
export MASS_STRIPE__WEBHOOKSECRET="whsec_..."
export MASS_STRIPE__MONTHLYPRICEID="price_..."
```

Webhook signature validation is **enforced** to prevent replay attacks.

## 🚦 Rate Limiting

Default: **100 requests/minute per IP**

Configure:
```json
{
  "Security": {
    "EnableRateLimiting": true,
    "MaxRequestsPerMinute": 100
  }
}
```

Exceeded requests return `429 Too Many Requests`.

## 📝 Production Deployment Checklist

- [ ] Set `ASPNETCORE_ENVIRONMENT=Production`
- [ ] Generate and set `MASS_JWTSETTINGS__SECRETKEY` (32+ chars)
- [ ] Configure `MASS_CORS__ALLOWEDORIGINS` with exact domains
- [ ] Set Stripe keys via environment variables
- [ ] Enable HTTPS with valid SSL certificate
- [ ] Configure IP whitelisting if applicable
- [ ] Review and adjust rate limits
- [ ] Enable firewall rules (UDP 67, 69, 4011; TCP 443)
- [ ] Rotate secrets regularly (quarterly recommended)

## 🐛 Reporting Security Issues

**Do not** open public GitHub issues for security vulnerabilities.

Contact: **Tomy Tolledo** (via private channel)

## 📚 Security Best Practices

1. **Use HTTPS in production** - HTTP is only for development
2. **Rotate JWT secrets** every 90 days
3. **Monitor PXE event logs** for suspicious activity
4. **Use strong passwords** for admin accounts (12+ chars, mixed case, numbers, symbols)
5. **Whitelist known IP ranges** if PXE server is internal-only
6. **Keep boot files updated** from trusted sources only
7. **Run services with minimum privileges** - avoid running as Administrator/root

## 🔍 Security Audits

Last audit: **December 2025**  
Next scheduled audit: **March 2026**

## 📄 License

Mass Suite is licensed under the MIT License.  
Copyright © 2025 Tomy Tolledo
