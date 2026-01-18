# Mass Suite API Documentation

## Overview

The Mass Suite API is a RESTful interface powered by ASP.NET Core 10. It provides access to PXE services, agent management, and system configuration.

## Authentication

All API endpoints require a valid JWT token in the `Authorization` header:

```
Authorization: Bearer <your_token>
```

Tokens are obtained via the `/api/v1/auth/login` endpoint.

## Versioning

The API uses URL versioning. The current version is `v1`.
Base URL: `https://your-server/api/v1`

## Endpoints

### Agents

- **GET /api/v1/agents** - List all registered agents
- **GET /api/v1/agents/{id}** - Get details for a specific agent
- **POST /api/v1/agents/register** - Register a new agent
- **POST /api/v1/agents/heartbeat** - Send a heartbeat

### PXE Services

- **GET /api/v1/boot/config/{macAddress}** - Get boot configuration for a specific machine
- **GET /api/v1/images** - List available boot images

### System

- **GET /health** - System health status
- **GET /metrics** - Prometheus metrics (OpenTelemetry)

## Data Models

### AgentRegistrationRequest

```json
{
  "hostname": "string",
  "macAddress": "string",
  "ipAddress": "string",
  "osVersion": "string",
  "agentVersion": "string"
}
```

### AgentHeartbeatRequest

```json
{
  "agentId": "string",
  "status": "Idle|Busy|Error|Offline",
  "cpuUsage": 0.0,
  "memoryUsage": 0.0,
  "activeJobId": "string" // optional
}
```
