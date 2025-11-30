# TFTP Boot Files

This directory contains PXE network boot files served by the ProPXEServer TFTP service.

## Required Files

Please place the following boot files in this directory:

### BIOS Boot (Legacy)
- `mass.kpxe` - iPXE boot file for BIOS systems

### UEFI Boot
- `mass.efi` - iPXE boot file for UEFI x64 systems
- `mass-arm64.efi` - iPXE boot file for UEFI ARM64 systems

### Additional Files
You may also include:
- netboot.xyz menus and assets
- Custom boot configurations
- Additional architecture-specific boot files

## Boot File Sources

You can obtain these files from:
- **netboot.xyz**: https://netboot.xyz/downloads/
- **iPXE**: https://ipxe.org/download
- Custom build your own PXE boot files

## File Permissions

Ensure these files are:
- ✅ Readable by the TFTP service
- ✅ Not executable (security)
- ✅ World-readable (chmod 644) on Linux deployments
