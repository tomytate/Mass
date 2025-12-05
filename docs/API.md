# Mass Suite API Reference

## Overview

Mass Suite exposes REST APIs via ASP.NET Core for workflow management, device operations, and agent coordination.

**Base URL**: `http://localhost:5000/api/v1`

## Authentication

Bearer token authentication:

```http
Authorization: Bearer <token>
```

## Endpoints

### Health

```http
GET /health
```

Returns service health status.

### Workflows

#### List Workflows

```http
GET /api/v1/workflows
```

Response:
```json
[
  {
    "id": "burn-iso",
    "name": "Burn ISO to USB",
    "version": "1.0.0",
    "stepCount": 5,
    "lastRun": "2024-12-05T10:30:00Z"
  }
]
```

#### Get Workflow

```http
GET /api/v1/workflows/{id}
```

#### Execute Workflow

```http
POST /api/v1/workflows/{id}/execute
Content-Type: application/json

{
  "parameters": {
    "isoPath": "C:\\Images\\win11.iso",
    "targetDevice": "\\\\.\\PhysicalDrive2"
  }
}
```

Response:
```json
{
  "executionId": "abc123",
  "status": "Running",
  "startedAt": "2024-12-05T10:30:00Z"
}
```

### Devices

#### List USB Devices

```http
GET /api/v1/devices/usb
```

Response:
```json
[
  {
    "deviceId": "\\\\.\\PhysicalDrive2",
    "name": "SanDisk Ultra",
    "sizeBytes": 32000000000,
    "isRemovable": true
  }
]
```

### Agents

#### List Connected Agents

```http
GET /api/v1/agents
```

Response:
```json
[
  {
    "agentId": "a1b2c3d4",
    "name": "WORKSTATION-01",
    "status": "Online",
    "lastHeartbeat": "2024-12-05T10:29:55Z"
  }
]
```

#### Send Command to Agent

```http
POST /api/v1/agents/{agentId}/commands
Content-Type: application/json

{
  "command": "ExecuteWorkflow",
  "parameters": {
    "workflowId": "deploy-image"
  }
}
```

### Plugins

#### List Installed Plugins

```http
GET /api/v1/plugins
```

#### Install Plugin

```http
POST /api/v1/plugins/install
Content-Type: application/json

{
  "pluginId": "autowim"
}
```

## Error Responses

All errors follow RFC 7807 Problem Details:

```json
{
  "type": "https://mass.suite/errors/validation",
  "title": "Validation Error",
  "status": 400,
  "detail": "The 'isoPath' field is required.",
  "instance": "/api/v1/workflows/burn-iso/execute"
}
```

## Versioning

API versioning via URL path: `/api/v1/`, `/api/v2/`

Breaking changes increment major version. Minor versions maintain backward compatibility.

## Rate Limiting

| Tier | Requests/Min |
|------|--------------|
| Free | 60 |
| Pro | 300 |
| Enterprise | Unlimited |

Rate limit headers:
```http
X-RateLimit-Limit: 60
X-RateLimit-Remaining: 45
X-RateLimit-Reset: 1701777600
```
