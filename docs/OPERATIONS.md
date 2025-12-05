# Mass Suite Operations

## Deployment

### Local Development

```bash
dotnet run --project src/Mass.Dashboard
dotnet run --project src/Mass.Agent
```

### Docker

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY publish/ .
ENTRYPOINT ["dotnet", "Mass.Dashboard.dll"]
```

Build and run:
```bash
docker build -t mass-dashboard .
docker run -p 5000:8080 mass-dashboard
```

### Production

1. Build release artifacts:
   ```bash
   dotnet publish -c Release -o publish
   ```

2. Configure environment variables (see Configuration section)

3. Deploy to target server (IIS, Kestrel, container orchestrator)

4. Configure reverse proxy (nginx, Traefik) for TLS termination

## Configuration

### Environment Variables

| Variable | Description | Required |
|----------|-------------|----------|
| `ConnectionStrings__Default` | Database connection | Yes |
| `MASS_JWT_SECRET` | JWT signing key | Yes |
| `MASS_LOG_LEVEL` | Log verbosity | No |
| `MASS_CORS_ORIGINS` | Allowed CORS origins | No |

### appsettings.json

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  },
  "AllowedHosts": "*",
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://0.0.0.0:5000"
      }
    }
  }
}
```

## Observability

### Logging

Structured JSON logging to stdout (container-friendly):

```json
{
  "timestamp": "2024-12-05T10:30:00Z",
  "level": "Information",
  "message": "Workflow executed",
  "workflowId": "burn-iso",
  "duration": 45.2
}
```

### Metrics

Prometheus-compatible metrics exposed at `/metrics`:

- `mass_workflows_executed_total`
- `mass_agents_connected`
- `mass_usb_burns_total`
- `mass_request_duration_seconds`

### Health Checks

```http
GET /health
GET /health/ready
GET /health/live
```

## Backup and Restore

### Database

```bash
# SQLite backup
cp /data/mass.db /backup/mass-$(date +%Y%m%d).db

# SQL Server backup
sqlcmd -S server -Q "BACKUP DATABASE MassSuite TO DISK='backup.bak'"
```

### Configuration

- Environment variables: Export and store securely
- Workflows: YAML files in version control
- Plugins: Package references in manifest

## Scaling

### Horizontal Scaling

- Dashboard: Stateless, scale via load balancer
- Agents: Register to any available hub
- Database: Connection pooling, read replicas

### Performance Baselines

| Operation | Target | P99 |
|-----------|--------|-----|
| API response | < 100ms | < 500ms |
| Workflow start | < 1s | < 3s |
| Agent heartbeat | < 50ms | < 200ms |

## Troubleshooting

### Common Issues

| Issue | Solution |
|-------|----------|
| Agent not connecting | Check firewall, verify hub URL |
| USB burn fails | Run as administrator, check device |
| Dashboard slow | Check database connection pool |

### Diagnostic Commands

```bash
# Check agent connectivity
curl http://localhost:5000/api/v1/agents

# View logs
docker logs mass-dashboard --tail 100

# Check health
curl http://localhost:5000/health
```
