namespace ProUSB.Domain;
public enum BurnStrategy { RawSectorWrite, FileSystemCopy }
public enum PartitionStyle { MBR, GPT, Hybrid, SuperFloppy }
public enum IsoType { Windows, Linux, Hybrid, Unknown }
public enum DeviceRiskLevel { Safe, Caution, Critical, SystemLockdown }
public enum DeviceBusType { Unknown, USB, SD, MMC, SCSI, ATA, NVMe, Virtual }
