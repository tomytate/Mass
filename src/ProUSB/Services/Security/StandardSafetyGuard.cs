using ProUSB.Domain;
using ProUSB.Domain.Services;
using Mass.Core.Devices;
using System;
using System.IO;
using System.Linq;

namespace ProUSB.Services.Security;

public class StandardSafetyGuard : ISafetyGuard {
    public DeviceRiskLevel EvaluateRisk(UsbDeviceInfo d) {
        if (!string.Equals(d.BusType, "USB", StringComparison.OrdinalIgnoreCase))
            return DeviceRiskLevel.Critical;

        var deviceType = DeviceSignatureDatabase.DetectDeviceType(d.VendorId, d.ProductId);
        
        if (deviceType == DeviceType.ExternalHdd)
            return DeviceRiskLevel.Critical;
        
        if (deviceType == DeviceType.CardReader && d.TotalSize > 512L * 1024 * 1024 * 1024)
            return DeviceRiskLevel.Critical;

        string? sysRoot = Path.GetPathRoot(Environment.GetFolderPath(Environment.SpecialFolder.System));
        if (!string.IsNullOrEmpty(sysRoot) && d.MountPoints?.Any(m => m.StartsWith(sysRoot, StringComparison.OrdinalIgnoreCase)) == true)
            return DeviceRiskLevel.SystemLockdown;

        try {
            var systemDiskSize = new DriveInfo(sysRoot!).TotalSize;
            if (Math.Abs(systemDiskSize - d.TotalSize) < (1024L * 1024 * 512))
                return DeviceRiskLevel.Critical;
        } catch { }

        string[] cardReaderIndicators = { "card reader", "cardreader", "sd/mmc", "multi-card" };
        if (cardReaderIndicators.Any(indicator => 
            (d.DeviceId?.Contains(indicator, StringComparison.OrdinalIgnoreCase) ?? false) ||
            (d.FriendlyName?.Contains(indicator, StringComparison.OrdinalIgnoreCase) ?? false)))
        {
            if (d.TotalSize > 512L * 1024 * 1024 * 1024)
                return DeviceRiskLevel.Critical;
        }

        if (d.TotalSize > 2L * 1024 * 1024 * 1024 * 1024)
            return DeviceRiskLevel.Critical;

        return DeviceRiskLevel.Safe;
    }
}

