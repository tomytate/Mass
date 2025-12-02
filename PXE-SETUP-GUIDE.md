# Mass Suite PXE Boot Files - Installation Guide

## 📁 File Organization

Your Mass Suite PXE boot files have been copied and rebranded from netboot.xyz to:

```
c:\Users\Administrator\Desktop\Mass\src\ProPXEServer\ProPXEServer.API\wwwroot\pxe\
```

### File Mapping (Rebranded)

| Original File | Mass Suite File | Purpose |
|--------------|----------------|---------|
| `netboot.xyz.kpxe` | `masssuite.kpxe` | **BIOS Legacy** - Standard PXE boot |
| `netboot.xyz-undionly.kpxe` | `masssuite-undionly.kpxe` | BIOS with UNDI |
| `netboot.xyz.efi` | `masssuite.efi` | **UEFI 64-bit** - Modern systems |
| `netboot.xyz-snp.efi` | `masssuite-snp.efi` | UEFI with SNP driver |
| `netboot.xyz-snponly.efi` | `masssuite-snponly.efi` | UEFI SNP only |
| `netboot.xyz-arm64.efi` | `masssuite-arm64.efi` | **ARM64 UEFI** - Raspberry Pi, etc. |
| `netboot.xyz-arm64-snp.efi` | `masssuite-arm64-snp.efi` | ARM64 with SNP |
| `netboot.xyz.iso` | `masssuite.iso` | Bootable ISO image |
| `netboot.xyz.img` | `masssuite.img` | Disk image |
| `netboot.xyz.usb` | `masssuite.usb` | USB bootable |
| `netboot.xyz.dsk` | `masssuite.dsk` | Disk image |

---

## 🔧 DHCP Configuration

Configure your DHCP server to point clients to Mass Suite PXE server:

### For BIOS (Legacy) Systems:
```
DHCP Option 66: <Your Server IP>  (e.g., 192.168.1.100)
DHCP Option 67: masssuite.kpxe
```

### For UEFI Systems:
```
DHCP Option 66: <Your Server IP>
DHCP Option 67: masssuite.efi
```

### For ARM64 Systems:
```
DHCP Option 66: <Your Server IP>
DHCP Option 67: masssuite-arm64.efi
```

---

## ⚙️ ProPXEServer Configuration

Update `appsettings.json` in ProPXEServer.API:

```json
{
  "MassBootServer": {
    "AdvertisedIP": "192.168.1.100",  // ← Change to your server IP
    "TftpListenPort": 69,
    "DhcpListenPort": 67,
    "ProxyDhcpListenPort": 4011
  },
  "TftpSettings": {
    "RootPath": "wwwroot/pxe",
    "Port": 69,
    "BlockSize": 512
  }
}
```

---

## 🚀 Starting Mass Suite PXE Server

1. **Build the solution:**
   ```powershell
   dotnet build
   ```

2. **Run ProPXEServer.API:**
   ```powershell
   cd src\ProPXEServer\ProPXEServer.API
   dotnet run
   ```

3. **Verify TFTP server is running:**
   - Check logs for: `TFTP Server started successfully`
   - Should listen on port 69

4. **Test from client machine:**
   - Boot a computer via PXE
   - Should see "Mass Suite" boot menu

---

## 📊 File Recommendations by Architecture

| Client Type | Recommended File |
|------------|------------------|
| **Modern Desktop/Laptop (UEFI)** | `masssuite.efi` |
| **Legacy Desktop/Laptop (BIOS)** | `masssuite.kpxe` |
| **Server Hardware** | `masssuite-snp.efi` |
| **Raspberry Pi 4/5** | `masssuite-arm64.efi` |
| **Virtual Machines** | `masssuite.kpxe` or `masssuite.efi` |

---

## 🔍 Troubleshooting

### Client doesn't boot:
- Verify DHCP options 66 & 67 are set correctly
- Check firewall allows UDP port 69 (TFTP)
- Confirm server IP is correct in appsettings.json

### TFTP timeout:
- Ensure ProPXEServer.API is running
- Check Windows Firewall rules
- Verify files exist in `wwwroot\pxe\`

### Wrong architecture boots:
- UEFI systems need `.efi` files
- BIOS systems need `.kpxe` files
- Configure DHCP to serve different files based on client architecture

---

## 📝 Production Checklist

- [ ] Files copied to `wwwroot\pxe\`
- [ ] `appsettings.json` configured with correct IP
- [ ] Windows Firewall allows UDP 69, 67, 4011
- [ ] DHCP server configured with options 66 & 67
- [ ] ProPXEServer.API running and listening
- [ ] Tested boot from BIOS client
- [ ] Tested boot from UEFI client

---

**Mass Suite PXE Server is ready for production! 🎉**
