using System;
using System.Collections.Generic;
using System.Linq;

namespace ProUSB.Domain;

public record BootVerificationResult {
    private string _deviceName = "Unknown";
    public required string DeviceName {
        get => _deviceName;
        init => _deviceName = value ?? "Unknown";
    }
    
    private string _deviceId = "";
    public required string DeviceId {
        get => _deviceId;
        init => _deviceId = value ?? "";
    }
    
    public bool IsBootable { get; set; }
    public BootMode BootMode { get; set; }
    public bool HasValidMBRSignature { get; set; }
    public bool HasActivePartition { get; set; }
    public bool IsGPT { get; set; }
    public bool HasESP { get; set; }
    public BootloaderType DetectedBootloader { get; set; }
    
    public List<string> Warnings { get; init; } = new();
    public List<string> Details { get; init; } = new();
    
    public string GetSummary() {
        var bootable = IsBootable ? "✅ Bootable" : "❌ Not Bootable";
        var mode = BootMode != BootMode.Unknown ? $"({BootMode})" : "";
        var loader = DetectedBootloader != BootloaderType.None 
            ? $" - {DetectedBootloader}" 
            : "";
        
        return $"{bootable} {mode}{loader}".Trim();
    }
}

