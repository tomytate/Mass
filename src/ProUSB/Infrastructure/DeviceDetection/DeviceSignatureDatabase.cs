using System.Collections.Generic;
using System.Linq;

namespace ProUSB.Infrastructure.DeviceDetection;

public enum DeviceType {
    Unknown,
    UsbFlashDrive,
    ExternalHdd,
    CardReader,
    SdCard,
    MemoryStick
}

public record DeviceSignature {
    public ushort VendorId { get; init; }
    public ushort ProductId { get; init; }
    public DeviceType Type { get; init; }
    public string Description { get; init; } = "";
}

public static class DeviceSignatureDatabase {
    private static readonly List<DeviceSignature> Signatures = new() {
        new() { VendorId = 0x0011, ProductId = 0x7788, Type = DeviceType.CardReader, Description = "SCM Microsystems card reader" },
        new() { VendorId = 0x03eb, ProductId = 0x0000, Type = DeviceType.UsbFlashDrive, Description = "Atmel generic UFD" },
        new() { VendorId = 0x03f0, ProductId = 0x0000, Type = DeviceType.ExternalHdd, Description = "HP external HDD" },
        new() { VendorId = 0x0402, ProductId = 0x5621, Type = DeviceType.CardReader, Description = "ALi card reader" },
        new() { VendorId = 0x0409, ProductId = 0x0040, Type = DeviceType.CardReader, Description = "NEC card reader" },
        new() { VendorId = 0x0411, ProductId = 0x0000, Type = DeviceType.ExternalHdd, Description = "Buffalo external HDD" },
        new() { VendorId = 0x0424, ProductId = 0x0000, Type = DeviceType.CardReader, Description = "SMSC card reader" },
        new() { VendorId = 0x0480, ProductId = 0x0000, Type = DeviceType.CardReader, Description = "Toshiba card reader" },
        new() { VendorId = 0x0480, ProductId = 0xa006, Type = DeviceType.ExternalHdd, Description = "Toshiba external HDD" },
        new() { VendorId = 0x04c5, ProductId = 0x0000, Type = DeviceType.CardReader, Description = "Fujitsu card reader" },
        new() { VendorId = 0x04e8, ProductId = 0x0000, Type = DeviceType.ExternalHdd, Description = "Samsung external HDD" },
        new() { VendorId = 0x054c, ProductId = 0x0000, Type = DeviceType.MemoryStick, Description = "Sony Memory Stick" },
        new() { VendorId = 0x058f, ProductId = 0x0000, Type = DeviceType.CardReader, Description = "Alcor Micro card reader" },
        new() { VendorId = 0x059b, ProductId = 0x0000, Type = DeviceType.CardReader, Description = "Iomega card reader" },
        new() { VendorId = 0x059f, ProductId = 0x0000, Type = DeviceType.CardReader, Description = "LaCie card reader" },
        new() { VendorId = 0x05ac, ProductId = 0x0000, Type = DeviceType.ExternalHdd, Description = "Apple external device" },
        new() { VendorId = 0x05dc, ProductId = 0x0000, Type = DeviceType.CardReader, Description = "Lexar card reader" },
        new() { VendorId = 0x05e3, ProductId = 0x0000, Type = DeviceType.CardReader, Description = "Genesys Logic card reader" },
        new() { VendorId = 0x0644, ProductId = 0x0000, Type = DeviceType.CardReader, Description = "TEAC card reader" },
        new() { VendorId = 0x067b, ProductId = 0x0000, Type = DeviceType.CardReader, Description = "Prolific card reader" },
        new() { VendorId = 0x0718, ProductId = 0x0000, Type = DeviceType.CardReader, Description = "Imation card reader" },
        new() { VendorId = 0x0781, ProductId = 0x0000, Type = DeviceType.UsbFlashDrive, Description = "SanDisk UFD" },
        new() { VendorId = 0x0781, ProductId = 0x5530, Type = DeviceType.CardReader, Description = "SanDisk card reader" },
        new() { VendorId = 0x0789, ProductId = 0x0000, Type = DeviceType.CardReader, Description = "Logitec card reader" },
        new() { VendorId = 0x0930, ProductId = 0x0000, Type = DeviceType.UsbFlashDrive, Description = "Toshiba UFD" },
        new() { VendorId = 0x0951, ProductId = 0x0000, Type = DeviceType.UsbFlashDrive, Description = "Kingston UFD" },
        new() { VendorId = 0x0984, ProductId = 0x0000, Type = DeviceType.CardReader, Description = "Apricorn card reader" },
        new() { VendorId = 0x0a16, ProductId = 0x0000, Type = DeviceType.CardReader, Description = "Yakumo card reader" },
        new() { VendorId = 0x0bc2, ProductId = 0x0000, Type = DeviceType.ExternalHdd, Description = "Seagate external HDD" },
        new() { VendorId = 0x0bda, ProductId = 0x0000, Type = DeviceType.CardReader, Description = "Realtek card reader" },
        new() { VendorId = 0x0c76, ProductId = 0x0000, Type = DeviceType.CardReader, Description = "JMicron card reader" },
        new() { VendorId = 0x0d7d, ProductId = 0x0000, Type = DeviceType.CardReader, Description = "Phison card reader" },
        new() { VendorId = 0x0dd8, ProductId = 0x0000, Type = DeviceType.CardReader, Description = "Netac card reader" },
        new() { VendorId = 0x0ea0, ProductId = 0x0000, Type = DeviceType.CardReader, Description = "Ours Technology card reader" },
        new() { VendorId = 0x0f88, ProductId = 0x0000, Type = DeviceType.CardReader, Description = "VTech card reader" },
        new() { VendorId = 0x1005, ProductId = 0x0000, Type = DeviceType.CardReader, Description = "Apacer card reader" },
        new() { VendorId = 0x1058, ProductId = 0x0000, Type = DeviceType.ExternalHdd, Description = "Western Digital external HDD" },
        new() { VendorId = 0x1307, ProductId = 0x0000, Type = DeviceType.CardReader, Description = "USBest card reader" },
        new() { VendorId = 0x13fd, ProductId = 0x0000, Type = DeviceType.CardReader, Description = "Initio card reader" },
        new() { VendorId = 0x1516, ProductId = 0x0000, Type = DeviceType.CardReader, Description = "Compro card reader" },
        new() { VendorId = 0x152d, ProductId = 0x0000, Type = DeviceType.ExternalHdd, Description = "JMicron external HDD" },
        new() { VendorId = 0x154b, ProductId = 0x0000, Type = DeviceType.UsbFlashDrive, Description = "PNY UFD" },
        new() { VendorId = 0x1f75, ProductId = 0x0000, Type = DeviceType.CardReader, Description = "Innostor card reader" },
        new() { VendorId = 0x8564, ProductId = 0x0000, Type = DeviceType.UsbFlashDrive, Description = "Transcend UFD" },
        new() { VendorId = 0x8644, ProductId = 0x0000, Type = DeviceType.CardReader, Description = "Generic card reader" },
    };

    public static DeviceType DetectDeviceType(ushort vendorId, ushort productId) {
        var exactMatch = Signatures.FirstOrDefault(s => s.VendorId == vendorId && s.ProductId == productId);
        if (exactMatch != null) {
            return exactMatch.Type;
        }

        var vendorMatch = Signatures.FirstOrDefault(s => s.VendorId == vendorId && s.ProductId == 0x0000);
        if (vendorMatch != null) {
            return vendorMatch.Type;
        }

        return DeviceType.Unknown;
    }

    public static string GetDeviceDescription(ushort vendorId, ushort productId) {
        var exactMatch = Signatures.FirstOrDefault(s => s.VendorId == vendorId && s.ProductId == productId);
        if (exactMatch != null) {
            return exactMatch.Description;
        }

        var vendorMatch = Signatures.FirstOrDefault(s => s.VendorId == vendorId && s.ProductId == 0x0000);
        return vendorMatch?.Description ?? "Unknown device";
    }

    public static bool IsKnownSafeUfd(ushort vendorId, ushort productId) {
        var type = DetectDeviceType(vendorId, productId);
        return type == DeviceType.UsbFlashDrive;
    }

    public static bool IsKnownDangerousDevice(ushort vendorId, ushort productId) {
        var type = DetectDeviceType(vendorId, productId);
        return type == DeviceType.ExternalHdd || type == DeviceType.CardReader;
    }
}

