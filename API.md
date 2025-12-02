# ProPXEServer API Documentation

## Overview
ProPXEServer provides a RESTful API for managing PXE boot infrastructure, including authentication, boot file management, and subscription handling.

**Base URL**: `https://localhost:7001/api`  
**Authentication**: JWT Bearer Token

---

## Authentication Endpoints

### Register New User
**POST** `/auth/register`

Creates a new user account.

**Request Body:**
```json
{
  "email": "admin@example.com",
  "password": "SecurePassword123!"
}
```

**Response (200 OK):**
```json
{
  "message": "User created successfully"
}
```

**Response (400 Bad Request):**
```json
{
  "errors": {
    "Password": ["Password must be at least 6 characters"]
  }
}
```

---

### Login
**POST** `/auth/login`

Authenticates a user and returns a JWT token.

**Request Body:**
```json
{
  "email": "admin@example.com",
  "password": "SecurePassword123!"
}
```

**Response (200 OK):**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiration": "2025-11-28T03:48:00Z"
}
```

**Response (401 Unauthorized):**
```json
{
  "message": "Invalid credentials"
}
```

---

## Boot Files Management

### List All Boot Files
**GET** `/bootfiles`

Returns a list of all boot files organized by architecture.

**Headers:**
```
Authorization: Bearer {token}
```

**Response (200 OK):**
```json
{
  "bios": [
    {
      "filename": "mass.kpxe",
      "size": 386679,
      "architecture": "BIOS",
      "lastModified": "2025-11-28T02:30:00Z"
    }
  ],
  "uefi": [
    {
      "filename": "mass.efi",
      "size": 1124352,
      "architecture": "UEFI",
      "lastModified": "2025-11-28T02:30:00Z"
    }
  ],
  "arm64": [
    {
      "filename": "mass-arm64.efi",
      "size": 1116160,
      "architecture": "ARM64",
      "lastModified": "2025-11-28T02:30:00Z"
    }
  ]
}
```

---

### Upload Boot File
**POST** `/bootfiles/upload`

Uploads a new boot file. The file is automatically categorized by architecture based on its name and extension.

**Headers:**
```
Authorization: Bearer {token}
Content-Type: multipart/form-data
```

**Request Body (Form Data):**
- `file`: Binary file data

**Response (200 OK):**
```json
{
  "message": "File uploaded successfully",
  "filename": "mass.kpxe",
  "architecture": "BIOS",
  "path": "wwwroot/pxe/bios/mass.kpxe"
}
```

**Automatic Architecture Detection:**
- `.kpxe`, `.lkrn`, `.dsk` → `bios/`
- `.efi` → `uefi/`
- `-arm64.efi` → `arm64/`

---

### Download Boot File
**GET** `/bootfiles/download/{filename}`

Downloads a specific boot file.

**Headers:**
```
Authorization: Bearer {token}
```

**Parameters:**
- `filename`: Name of the file to download (e.g., `mass.kpxe`)

**Response (200 OK):**
- Binary file stream
- Content-Type: `application/octet-stream`

**Response (404 Not Found):**
```json
{
  "message": "File not found"
}
```

---

### Delete Boot File
**DELETE** `/bootfiles/{filename}`

Deletes a boot file.

**Headers:**
```
Authorization: Bearer {token}
```

**Parameters:**
- `filename`: Name of the file to delete

**Response (200 OK):**
```json
{
  "message": "File deleted successfully"
}
```

**Response (404 Not Found):**
```json
{
  "message": "File not found"
}
```

---

## Distribution Images

### List All Distribution Images
**GET** `/images`

Returns a list of all available distribution images (USB, ISO, disk images).

**Response (200 OK):**
```json
[
  {
    "id": 1,
    "fileName": "mass.iso",
    "category": "ISO",
    "fileSizeBytes": 2402304,
    "fileSizeMB": 2.29,
    "downloadUrl": "/api/images/iso/mass.iso",
    "createdAt": "2025-11-28T10:00:00Z"
  },
  {
    "id": 2,
    "fileName": "mass.img",
    "category": "USB",
    "fileSizeBytes": 2064384,
    "fileSizeMB": 1.97,
    "downloadUrl": "/api/images/usb/mass.img",
    "createdAt": "2025-11-28T10:00:00Z"
  }
]
```

---

### Download USB Image
**GET** `/images/usb/{filename}`

Downloads a USB bootable image file.

**Parameters:**
- `filename`: Name of the USB image (e.g., `mass.img`, `mass-multiarch.img`)

**Response (200 OK):**
- Binary file stream
- Content-Type: `application/octet-stream`

**Available USB Images:**
- `mass.img` - Standard USB image (~2 MB)
- `mass-multiarch.img` - Multi-architecture USB image (~3 MB)

---

### Download ISO Image
**GET** `/images/iso/{filename}`

Downloads an ISO bootable image file.

**Parameters:**
- `filename`: Name of the ISO image

**Response (200 OK):**
- Binary file stream
- Content-Type: `application/x-iso9660-image`

**Available ISO Images:**
- `mass.iso` - Standard ISO (~2.4 MB)
- `mass-multiarch.iso` - Multi-architecture ISO (~3.5 MB)
- `mass-arm64.iso` - ARM64 ISO (~2.0 MB)

---

### Download Disk Image
**GET** `/images/disk/{filename}`

Downloads a disk image file.

**Parameters:**
- `filename`: Name of the disk image

**Response (200 OK):**
- Binary file stream
- Content-Type: `application/octet-stream`

**Available Disk Images:**
- `mass.dsk` - Standard disk image
- `mass.efi.dsk` - UEFI disk image

---

## Subscription Endpoints

### Create Checkout Session
**POST** `/subscription/create-checkout`

Creates a Stripe checkout session for monthly subscription.

**Headers:**
```
Authorization: Bearer {token}
```

**Response (200 OK):**
```json
{
  "sessionId": "cs_test_...",
  "url": "https://checkout.stripe.com/pay/cs_test_..."
}
```

**Usage:**
Redirect user to the `url` to complete payment.

---

### Stripe Webhook
**POST** `/subscription/webhook`

Handles Stripe payment events.

**Headers:**
```
Stripe-Signature: t=...,v1=...
```

**Events Handled:**
- `checkout.session.completed` - Activates subscription
- `customer.subscription.updated` - Updates subscription status
- `customer.subscription.deleted` - Deactivates subscription

**Response (200 OK):**
```json
{
  "received": true
}
```

---

## PXE Events (Internal Logging)

While there's no direct API endpoint for PXE events, they are logged in the database and can be queried:

**PxeEvent Model:**
```csharp
{
  "Id": 1,
  "EventType": "DHCP_DISCOVER",
  "MacAddress": "00:11:22:33:44:55",
  "IpAddress": "192.168.1.50",
  "Architecture": "0x0007",
  "Timestamp": "2025-11-28T02:48:00Z"
}
```

**Event Types:**
- `DHCP_DISCOVER` - Client discovery
- `DHCP_OFFER` - Server offer sent
- `PROXY_DHCP` - ProxyDHCP request
- `TFTP_READ` - TFTP file request

---

## Boot Configuration (Future)

The system includes a `BootConfiguration` model for managing custom boot menus:

```csharp
{
  "Id": 1,
  "Name": "Ubuntu 24.04 LTS",
  "Architecture": "UEFI",
  "KernelPath": "/ubuntu/vmlinuz",
  "InitrdPath": "/ubuntu/initrd",
  "KernelParams": "root=/dev/nfs nfsroot=192.168.1.100:/ubuntu",
  "IsEnabled": true
}
```

**Note**: API endpoints for boot configuration management are planned for future releases.

---

## Error Responses

All endpoints follow a consistent error response format:

**400 Bad Request:**
```json
{
  "errors": {
    "FieldName": ["Error message"]
  }
}
```

**401 Unauthorized:**
```json
{
  "message": "Unauthorized"
}
```

**403 Forbidden:**
```json
{
  "message": "Subscription required"
}
```

**404 Not Found:**
```json
{
  "message": "Resource not found"
}
```

**500 Internal Server Error:**
```json
{
  "message": "An error occurred",
  "details": "Stack trace (dev mode only)"
}
```

---

## Rate Limiting

- **Default**: 100 requests per minute per IP
- **Configuration**: `Security.MaxRequestsPerMinute` in `appsettings.json`
- **Response Header**: `X-Rate-Limit-Remaining`

**Response (429 Too Many Requests):**
```json
{
  "message": "Rate limit exceeded"
}
```

---

## Security Considerations

1. **Always use HTTPS in production**
2. **Rotate JWT secret keys regularly**
3. **Implement IP whitelisting for sensitive operations**
4. **Monitor PXE events for suspicious activity**
5. **Keep Stripe webhook secret secure**
6. **Use strong password policies**
7. **Enable CORS only for trusted domains**

---

## Sample Client Code

### C# Example
```csharp
using System.Net.Http.Headers;
using System.Net.Http.Json;

var client = new HttpClient { BaseAddress = new Uri("https://localhost:7001") };

// Login
var loginData = new { email = "admin@example.com", password = "pass" };
var response = await client.PostAsJsonAsync("/api/auth/login", loginData);
var result = await response.Content.ReadFromJsonAsync<LoginResponse>();

// Use token
client.DefaultRequestHeaders.Authorization = 
    new AuthenticationHeaderValue("Bearer", result.Token);

// Get boot files
var bootFiles = await client.GetFromJsonAsync<BootFilesResponse>("/api/bootfiles");
```

### JavaScript Example
```javascript
// Login
const response = await fetch('https://localhost:7001/api/auth/login', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({ 
    email: 'admin@example.com', 
    password: 'pass' 
  })
});
const { token } = await response.json();

// Get boot files
const bootFilesResponse = await fetch('https://localhost:7001/api/bootfiles', {
  headers: { 'Authorization': `Bearer ${token}` }
});
const bootFiles = await bootFilesResponse.json();
```

---

## Versioning
Current API Version: **v1.0.0**

Future versions will be accessible via URL prefix:
- `/api/v1/auth/login`
- `/api/v2/auth/login`
