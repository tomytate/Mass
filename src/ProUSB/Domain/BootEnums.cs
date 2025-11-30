namespace ProUSB.Domain;

public enum BootMode {
    Unknown,
    Legacy,
    UEFI,
    Hybrid
}

public enum BootloaderType {
    None,
    Unknown,
    WindowsBoot,
    GRUB,
    Syslinux,
    UEFIGeneric
}

